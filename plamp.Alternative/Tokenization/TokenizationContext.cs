using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using plamp.Abstractions.Ast;
using plamp.Alternative.Tokenization.Token;

namespace plamp.Alternative.Tokenization;

internal class TokenizationContext : IDisposable
{
    private record struct CharBufferEntry(char Symbol, byte ByteLen);
    
    private long _byteOffset;
    private readonly Encoding _encoding;
    private readonly List<TokenBase> _tokens;
    private readonly List<PlampException> _exceptions;
    private readonly string _fileName;

    private const int MaxPeekLength = 4;

    private readonly Stream _fileStream;
    
    private readonly CharBufferEntry[] _charPrefetch; // 2 KiB
    private int _prefetchedCount;
    private int _prefetchedIndex;
    
    private readonly byte[] _readBuffer = new byte[1024]; // 1 KiB
    private int _readBufferCount;
    private int _readBufferIndex;
    
    private readonly Decoder _decoder;

    private bool _disposed;
    private bool _isEof;
    private IAsyncEnumerator<char> SourceFileEnumerator { get; }

    public List<TokenBase> Tokens
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _tokens;
        }
    }

    public List<PlampException> Exceptions
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _exceptions;
        }
    }

    public string FileName
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _fileName;
        }
    }

    public long ByteOffset
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _byteOffset;
        }
    }

    public Encoding Encoding
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _encoding;
        }
    }

    public bool IsEof
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _isEof;
        }
    }

    public async ValueTask<bool> IsEol()
    {
        if(Current == '\n') return true;
        if (Current != '\r') return false;
        var next = await PeekCharAtAsync(1);
        return next == '\n';
    }
    
    public ValueTask<bool> IsExprEnd
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return IsEof ? new ValueTask<bool>(true) : IsEol();
        }
    }

    public TokenizationContext(
        List<TokenBase> tokens,
        List<PlampException> exceptions,
        string fileName,
        Stream fileStream,
        Encoding encoding,
        CancellationToken ct = default)
    {
        _fileStream = fileStream;
        if (!fileStream.CanRead) throw new ArgumentException($"{nameof(fileName)} должно быть возможно читать");

        _decoder = encoding.GetDecoder();
        _tokens = tokens;
        _exceptions = exceptions;
        _fileName = fileName;
        _encoding = encoding;
        SourceFileEnumerator = GetAsyncEnumerator(ct);
        _charPrefetch = new CharBufferEntry[_encoding.GetMaxCharCount(_readBuffer.Length)];
    }

    ~TokenizationContext() => Dispose();

    public ValueTask<bool> MoveNextAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return SourceFileEnumerator.MoveNextAsync();
    }

    public char Current
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return SourceFileEnumerator.Current;
        }
    }

    private void ShiftUnreadToBeginning(Array buffer, ref int index, ref int limit)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, limit);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, buffer.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        if(index == 0) return;
        if (limit == index)
        {
            limit = 0;
            index = 0;
            return;
        }
        
        Buffer.BlockCopy(buffer, index, buffer, 0, limit - index);
        limit -= index;
        index = 0;
    }

    private async Task FillPrefetchBufferAsync(CancellationToken ct = default)
    {
        var tempBuffer = new char[_encoding.GetMaxCharCount(1024)];
        while (_fileStream.Position < _fileStream.Length || _readBufferCount != 0)
        {
            ShiftUnreadToBeginning(_readBuffer, ref _readBufferCount, ref _readBufferIndex);
            ShiftUnreadToBeginning(_charPrefetch, ref _prefetchedIndex, ref _prefetchedCount);
            var bytesRead = await _fileStream.ReadAsync(_readBuffer, _readBufferCount, _readBuffer.Length - _readBufferCount, ct);
            _readBufferCount += bytesRead;
            
            _decoder.Convert(
                _readBuffer, 
                _readBufferIndex, 
                _readBufferCount - _readBufferIndex, 
                tempBuffer, 
                0, 
                Math.Min(tempBuffer.Length, _charPrefetch.Length - _prefetchedCount), 
                bytesRead == 0, 
                out bytesRead, 
                out var charsRead, 
                out _);
            
            _readBufferIndex += bytesRead;
            for (var i = 0; i < charsRead; i++)
            {
                var charLen = _encoding.GetByteCount(tempBuffer, i, 1);
                _charPrefetch[_prefetchedCount++] = new(tempBuffer[i], (byte)charLen);
            }
        }
    }
    
    public async Task<char?> PeekCharAtAsync(int offset, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, MaxPeekLength);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(offset, 0);
        var prefetchIx = _prefetchedIndex + offset - 1;
        
        if(prefetchIx < _prefetchedCount) return _charPrefetch[prefetchIx].Symbol;
        ShiftUnreadToBeginning(_charPrefetch, ref _prefetchedIndex, ref _prefetchedCount);
        await FillPrefetchBufferAsync(ct);
        return prefetchIx < _prefetchedCount ? _charPrefetch[prefetchIx].Symbol : null;
    }

    private async Task<CharBufferEntry?> TryReadFromBufferedStream(CancellationToken ct)
    {
        if(_prefetchedIndex < _prefetchedCount) return _charPrefetch[_prefetchedIndex++];
        await FillPrefetchBufferAsync(ct);
        return _prefetchedIndex < _prefetchedCount ? _charPrefetch[_prefetchedIndex++] : null;
    }
    
    private async IAsyncEnumerator<char> GetAsyncEnumerator(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        CharBufferEntry? current = null;
        do
        {
            var next = await TryReadFromBufferedStream(ct);
            _byteOffset += current?.ByteLen ?? 0;
            
            if(next != null)
            {
                current = next;
                yield return next.Value.Symbol;
            }
            else
            {
                _isEof = true;
                yield break;
            }
        } while (true);
    }


    public void Dispose()
    {
        if(_disposed) return;
        
        _disposed = true;
        _fileStream.Dispose();
    }
}
