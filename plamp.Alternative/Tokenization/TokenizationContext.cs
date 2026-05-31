using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using plamp.Abstractions.Ast;
using plamp.Alternative.Tokenization.Token;

namespace plamp.Alternative.Tokenization;

/// <summary>
/// Хранит всё состояние во время токенизации файла, одноразовый, однопроходный. Не закрывает исходный стрим из которого получает данные.
/// Не совместим с суррогатами юникода.
/// </summary>
internal class TokenizationContext : IDisposable
{
    /// <summary>
    /// Запись, которая хранит символ и его положение в кодовом файле.
    /// </summary>
    /// <param name="Symbol">Utf-16 представление символа</param>
    /// <param name="ByteLen">Байтовое смещение в кодовом файле</param>
    private record struct CharBufferEntry(char Symbol, int ByteLen);

    /// <summary>
    /// Максимальное число символов, на которое можно сделать <see cref="PeekCharAtAsync"/> с текущего <see cref="Current"/> символа.
    /// Большее значение выдаст <see cref="InvalidOperationException"/>
    /// </summary>
    public const int MaxPeekLength = 4;

    /// <summary>
    /// Длина байтового буфера использующегося при чтении из стрима.
    /// Длина буферизации char зависит от кодировки и равна максимальному числу char, которое можно уместить в такое число байт.
    /// </summary>
    public const int ByteBufferLength = 1024;

    /// <summary>
    /// Смещение, которое соответствует текущему(<see cref="Current"/>) символу
    /// </summary>
    private long _byteOffset;

    /// <summary>
    /// Кодировка, в которой представлен исходный файл
    /// </summary>
    private readonly Encoding _encoding;

    /// <summary>
    /// Список токенов, в который их записывает токенайзер во время токенизации
    /// </summary>
    private readonly List<TokenBase> _tokens;

    /// <summary>
    /// Список ошибок токенизации, которые токенайзер записывает во время токенизации исходного файла
    /// </summary>
    private readonly List<PlampException> _exceptions;

    /// <summary>
    /// Имя исходного файла, который токенизируется
    /// </summary>
    private readonly string _fileName;

    /// <summary>
    /// Текущий стрим, на базе которого происходит перечисление char в текущем классе.
    /// После завершения токенизации остаётся открытым/
    /// </summary>
    private readonly Stream _fileStream;
    
    /// <summary>
    /// Prefetch buffer, служит для пакетной загрузки данных из файлового стрима.
    /// Равен максимальному числу символов, которые могут поместиться в 1KiB в текущей кодировке
    /// </summary>
    private readonly CharBufferEntry[] _charPrefetch;
    
    /// <summary>
    /// Сколько символов сейчас находится в буфере
    /// </summary>
    private int _prefetchedCount;
    
    /// <summary>
    /// Сколько текущий символ, который указывает на первый не потреблённый символ. Должен быть &lt;= <see cref="_prefetchedCount"/>
    /// </summary>
    private int _prefetchedIndex;
    
    /// <summary>
    /// Буфер для пакетного чтения из файлового стрима
    /// </summary>
    private readonly byte[] _readBuffer = new byte[ByteBufferLength];
    
    /// <summary>
    /// Сколько символов сейчас находится в буфере
    /// </summary>
    private int _readBufferCount;
    
    /// <summary>
    /// Текущая позиция последнего не поглощённого байта. Должна быть &lt;= <see cref="_readBuffer"/>
    /// </summary>
    private int _readBufferIndex;

    /// <summary>
    /// Буфер в который копируются символы из <see cref="_decoder"/> при пакетном декодировании.
    /// Равен максимальному числу символов, которые могут поместиться в 1KiB в текущей кодировке.
    /// </summary>
    private readonly char[] _temporaryCharBuffer;
    
    /// <summary>
    /// Потоковый декодер для декодирования стрима в определённой кодировке.
    /// </summary>
    private readonly Decoder _decoder;

    /// <summary>
    /// Завершён ли жизненный цикл объекта. После этого нельзя взаимодействовать с публичным api объекта
    /// </summary>
    private bool _disposed;
    
    /// <summary>
    /// Завершено ли прочтение файла
    /// </summary>
    private bool _isEof;
    
    /// <summary>
    /// Флаг, который сигнализирует, что следует вытеснить данные из буферов декодера. Ставится в true, если в предыдущей итерации декодер не до конца декодировал символ.
    /// </summary>
    private bool _needToFlush;
    
    /// <summary>
    /// Длина текущего не использованного символа. Служит для сохранения при перечислении.
    /// </summary>
    private int _currentByteLength;
    
    /// <summary>
    /// Внутренний перечислитель, который идёт по каждому символу в файле
    /// </summary>
    private IAsyncEnumerator<char> SourceFileEnumerator { get; }

    /// <inheritdoc cref="_tokens"/>
    /// <exception cref="ObjectDisposedException">Жизненный цикл объекта завершён</exception>
    public List<TokenBase> Tokens
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _tokens;
        }
    }

    /// <inheritdoc cref="_exceptions"/>
    /// <exception cref="ObjectDisposedException">Жизненный цикл объекта завершён</exception>
    public List<PlampException> Exceptions
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _exceptions;
        }
    }

    /// <inheritdoc cref="_fileName"/>
    /// <exception cref="ObjectDisposedException">Жизненный цикл объекта завершён</exception>
    public string FileName
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _fileName;
        }
    }

    /// <inheritdoc cref="_byteOffset"/>
    /// <exception cref="ObjectDisposedException">Жизненный цикл объекта завершён</exception>
    public long ByteOffset
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _byteOffset;
        }
    }

    /// <inheritdoc cref="_encoding"/>
    /// <exception cref="ObjectDisposedException">Жизненный цикл объекта завершён</exception>
    public Encoding Encoding
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _encoding;
        }
    }

    /// <inheritdoc cref="_isEof"/>
    /// <exception cref="ObjectDisposedException">Жизненный цикл объекта завершён</exception>
    public bool IsEof
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _isEof;
        }
    }
    
    /// <summary>
    /// Является ли текущий символ переносом строки или прочтений исходного файла завершено
    /// </summary>
    /// <exception cref="ObjectDisposedException">Жизненный цикл объекта завершён</exception>
    public ValueTask<bool> IsExprEnd
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return IsEof ? new ValueTask<bool>(true) : IsEol();
        }
    }
    
    /// <summary>
    /// Текущий символ на котором сейчас находится перечисление файла.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Жизненный цикл объекта завершён</exception>
    public char Current
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return SourceFileEnumerator.Current;
        }
    }

    /// <summary>
    /// Байтовая длина текущего символа
    /// </summary>
    /// <exception cref="ObjectDisposedException">Жизненный цикл объекта завершён</exception>
    public int CurrentByteLength
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _currentByteLength;
        }
    }

    /// <summary>
    /// Основной конструктор контекста токенизации
    /// </summary>
    /// <param name="tokens">Список токенов</param>
    /// <param name="exceptions">Список ошибок обнаруженных при токенизации</param>
    /// <param name="fileName">Имя исходного файла может быть пустым</param>
    /// <param name="fileStream">Стрим с данными исходного файла обязан быть readable</param>
    /// <param name="encoding">Кодировка текущего исходного файла</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <exception cref="ArgumentException">Возникает если исходный файл не readable</exception>
    internal TokenizationContext(
        List<TokenBase> tokens,
        List<PlampException> exceptions,
        string fileName,
        Stream fileStream,
        Encoding encoding,
        CancellationToken ct = default)
    {
        if (!fileStream.CanRead) throw new ArgumentException($"{nameof(fileName)} должно быть возможно читать");

        _fileStream = fileStream;
        _decoder = encoding.GetDecoder();
        _tokens = tokens;
        _exceptions = exceptions;
        _fileName = fileName;
        _encoding = encoding;
        SourceFileEnumerator = GetAsyncEnumerator(ct);
        _charPrefetch = new CharBufferEntry[_encoding.GetMaxCharCount(_readBuffer.Length)];
        _temporaryCharBuffer = new char[_charPrefetch.Length];
    }
    
    /// <summary>
    /// Является ли текущая последовательность переносом строки. Распознаются только LF и CRLF окончания.
    /// </summary>
    /// <returns>Флаг (не)наличия символа переноса строки.</returns>
    /// <exception cref="ObjectDisposedException">Жизненный цикл объекта завершён</exception>
    public async ValueTask<bool> IsEol()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if(Current == '\n') return true;
        if (Current != '\r') return false;
        var next = await PeekCharAtAsync(1);
        return next == '\n';
    }

    /// <summary>
    /// Переместиться на следующий символ в исходном файле.
    /// </summary>
    /// <returns>Флаг успеха операции, false - если достигнут конец исходного файла</returns>
    /// <exception cref="ObjectDisposedException">Жизненный цикл объекта завершён</exception>
    public ValueTask<bool> MoveNextAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return SourceFileEnumerator.MoveNextAsync();
    }

    /// <summary>
    /// Перенести непрочитанную часть (index &lt;= i &lt; limit)
    /// Если index == limit, то оба этих значения будут установлены в 0, а копирование памяти не произойдёт.
    /// Если index == 0 операция ничего не делает.
    /// </summary>
    /// <param name="buffer">Массив, над которым происходи операция</param>
    /// <param name="index">Первый актуальный для копирования элемент</param>
    /// <param name="limit">Последний актуальный для копирования элемент (не включительно)</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Если <see cref="index"/> &gt; <see cref="limit"/>
    /// || <see cref="limit"/> &gt; длины <see cref="buffer"/>  
    /// || <see cref="limit"/> &lt; 0  
    /// || <see cref="index"/> &lt; 0  
    /// </exception>
    private void ShiftUnreadToBeginning(Array buffer, ref int index, ref int limit)
    {
        //Валидация
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, limit);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, buffer.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        
        //Крайние случаи
        if(index == 0) return;
        if (limit == index)
        {
            limit = 0;
            index = 0;
            return;
        }
        
        //Перенос
        Array.Copy(buffer, index, buffer, 0, limit - index);
        
        //Установка новых значений
        limit -= index;
        index = 0;
    }

    /// <summary>
    /// Заполнение <see cref="_charPrefetch"/> новыми символами.
    /// </summary>
    private async Task FillPrefetchBufferAsync(CancellationToken ct = default)
    {
        ShiftUnreadToBeginning(_charPrefetch, ref _prefetchedIndex, ref _prefetchedCount);
        
        //Попытки заполнения происходят пока в prefetch буфере есть место и хотя бы в 1 из источников информации есть данные.
        while (_prefetchedCount < _charPrefetch.Length
               && (_fileStream.Position < _fileStream.Length
                   || _readBufferIndex < _readBufferCount
                   || _needToFlush))
        {
            //Каждую итерацию сдвигаем информацию в буфере байтов
            ShiftUnreadToBeginning(_readBuffer, ref _readBufferIndex, ref _readBufferCount);

            //Дозаписываем информацию в буфер из стрима
            var bytesRead = await _fileStream.ReadAsync(_readBuffer, _readBufferCount, _readBuffer.Length - _readBufferCount, ct);
            _readBufferCount += bytesRead;
            
            //Конвертирует данные из буфера байтов во временный char буфер
            _decoder.Convert(
                _readBuffer, 
                _readBufferIndex, 
                _readBufferCount - _readBufferIndex, 
                _temporaryCharBuffer, 
                0, 
                Math.Min(_temporaryCharBuffer.Length, _charPrefetch.Length - _prefetchedCount), 
                _needToFlush && bytesRead == 0, 
                out bytesRead, 
                out var charsRead, 
                out var completed);
            
            _needToFlush = !completed;
            _readBufferIndex += bytesRead;
            
            //Записывает данные в prefetch буфер
            for (var i = 0; i < charsRead; i++)
            {
                var charLen = _encoding.GetByteCount(_temporaryCharBuffer, i, 1);
                _charPrefetch[_prefetchedCount++] = new(_temporaryCharBuffer[i], charLen);
            }
        }
    }
    
    /// <summary>
    /// Prefetch логика для получения будущих символов в файле.
    /// </summary>
    /// <param name="offset">Смещение от <see cref="Current"/> символа, строго больше 0 и меньше или равно <see cref="MaxPeekLength"/></param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Возвращает prefetch символ или null если достигнут конец файла.</returns>
    /// <exception cref="ObjectDisposedException">Жизненный цикл объекта завершён</exception>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="offset"/> меньше или равен 0 или <see cref="offset"/> больше <see cref="MaxPeekLength"/></exception>
    public async Task<char?> PeekCharAtAsync(int offset, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, MaxPeekLength);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(offset, 0);
        
        //Пытаемся сделать быстрый prefetch если в буфере имеется достаточно данных.
        var prefetchIx = _prefetchedIndex + offset - 1;
        if(prefetchIx < _prefetchedCount) return _charPrefetch[prefetchIx].Symbol;
        
        //Если не удалось, то заполняем его новыми данными
        await FillPrefetchBufferAsync(ct);
        
        //И снова делаем prefetch
        prefetchIx = _prefetchedIndex + offset - 1;
        return prefetchIx < _prefetchedCount ? _charPrefetch[prefetchIx].Symbol : null;
    }

    /// <summary>
    /// Логика как в <see cref="PeekCharAtAsync"/> пытаемся читать сразу из буфера, если не удалось, то заполняем его новыми данными и ещё раз читаем.
    /// </summary>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Новый символ или null если достигнут конец файла</returns>
    private async Task<CharBufferEntry?> TryReadFromBufferedStream(CancellationToken ct)
    {
        if(_prefetchedIndex < _prefetchedCount) return _charPrefetch[_prefetchedIndex++];
        await FillPrefetchBufferAsync(ct);
        return _prefetchedIndex < _prefetchedCount ? _charPrefetch[_prefetchedIndex++] : null;
    }
    
    /// <summary>
    /// Получить внутренний enumerator для удобной абстракции над файловым стримом.
    /// </summary>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Перечислитель char из <see cref="_fileStream"/></returns>
    private async IAsyncEnumerator<char> GetAsyncEnumerator(CancellationToken ct = default)
    {
        CharBufferEntry? current = null;
        do
        {
            var next = await TryReadFromBufferedStream(ct);
            
            //В начале файла в UTF-8 может располагаться маркер кодировки. Его следует пропускать, при этом смещение в стриме не изменяется, так как он должен иметь нулевую длину.
            if (_byteOffset == 0 && next is { Symbol: (char)65279 } && Encoding.Equals(Encoding.UTF8))
            {
                continue;
            }
            
            _byteOffset += current?.ByteLen ?? 0;
            
            if(next != null)
            {
                current = next;
                _currentByteLength = next.Value.ByteLen;
                yield return next.Value.Symbol;
            }
            else
            {
                _isEof = true;
                yield break;
            }
        } while (true);
    }
    
    /// <summary>
    /// Финализация объекта, не закрывает стрим, но запрещает использовать публичный api класса.
    /// </summary>
    public void Dispose()
    {
        if(_disposed) return;
        _disposed = true;
    }
}
