using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using plamp.Alternative.Tokenization;
using Xunit;

namespace plamp.Alternative.Tests.Tokenization;

/// <summary>
/// Тесты <see cref="TokenizationContext"/>
/// </summary>
public class TokenizationContextTests
{
    private const string FileName = "test.plp";

    /// <summary>
    /// Создание контекста с unreadable stream завершается ошибкой.
    /// </summary>
    [Fact]
    public void CreateWithUnreadableStream_ThrowsArgumentException()
    {
        using var stream = new UnreadableStream();

        Assert.Throws<ArgumentException>(() => CreateContext(stream, Encoding.UTF8));
    }

    /// <summary>
    /// Если весь stream помещается в один буфер, контекст корректно вычитывает его до конца.
    /// </summary>
    [Fact]
    public async Task MoveNextAsync_WhenStreamFitsSingleBuffer_ReadsToEndCorrectly()
    {
        const string text = "abc";
        using var context = CreateContext(text, Encoding.UTF8);

        var result = await ReadAllAsync(context);

        Assert.Equal(text, result);
        Assert.True(context.IsEof);
        Assert.Equal(Encoding.UTF8.GetByteCount(text), context.ByteOffset);
    }

    /// <summary>
    /// Если stream не помещается в один буфер, контекст выполняет дополнительную буферизацию и вычитывает всё содержимое.
    /// </summary>
    [Fact]
    public async Task MoveNextAsync_WhenStreamDoesNotFitSingleBuffer_ReadsToEndCorrectly()
    {
        var text = new string('a', TokenizationContext.ByteBufferLength + 16);
        var bytes = Encoding.UTF8.GetBytes(text);
        await using var stream = new CountingReadStream(bytes);
        using var context = CreateContext(stream, Encoding.UTF8);

        var result = await ReadAllAsync(context);

        Assert.Equal(text, result);
        Assert.True(stream.ReadCallCount > 1);
        Assert.Equal(bytes.Length, context.ByteOffset);
    }

    /// <summary>
    /// Пустой stream корректно завершается без текущего символа и выставляет EOF.
    /// </summary>
    [Fact]
    public async Task MoveNextAsync_WhenStreamIsEmpty_ReturnsFalseAndSetsEof()
    {
        using var context = CreateContext(string.Empty, Encoding.UTF8);

        var moved = await context.MoveNextAsync();

        Assert.False(moved);
        Assert.True(context.IsEof);
        Assert.Equal(0, context.ByteOffset);
    }

    /// <summary>
    /// Если многобайтовый символ оказался на границе буферов, декодер дозаписывает его при следующей буферизации.
    /// </summary>
    [Fact]
    public async Task MoveNextAsync_WhenMultibyteSymbolIsSplitBetweenBuffers_CompletesSymbolOnNextBuffering()
    {
        var text = new string('a', TokenizationContext.ByteBufferLength - 1) + "Ёb";
        var bytes = Encoding.UTF8.GetBytes(text);
        await using var stream = new CountingReadStream(bytes);
        using var context = CreateContext(stream, Encoding.UTF8);

        var result = await ReadAllAsync(context);

        Assert.Equal(text, result);
        Assert.True(stream.ReadCallCount > 1);
        Assert.Equal(bytes.Length, context.ByteOffset);
    }

    /// <summary>
    /// Unicode surrogate pair не воспринимается как единый символ и возвращается двумя char.
    /// </summary>
    [Fact]
    public async Task MoveNextAsync_WhenUnicodeSurrogatePairRead_ReturnsTwoChars()
    {
        const string text = "😀";
        using var context = CreateContext(text, Encoding.UTF8);

        var firstMove = await context.MoveNextAsync();
        var first = context.Current;
        var secondMove = await context.MoveNextAsync();
        var second = context.Current;

        Assert.True(firstMove);
        Assert.True(secondMove);
        Assert.True(char.IsHighSurrogate(first));
        Assert.True(char.IsLowSurrogate(second));
    }

    /// <summary>
    /// После завершения чтения файла контекст выставляет IsEof.
    /// </summary>
    [Fact]
    public async Task MoveNextAsync_WhenEndReached_SetsIsEof()
    {
        using var context = CreateContext("a", Encoding.UTF8);

        Assert.True(await context.MoveNextAsync());
        Assert.False(await context.MoveNextAsync());

        Assert.True(context.IsEof);
    }

    /// <summary>
    /// LF в текущей позиции считается концом строки.
    /// </summary>
    [Fact]
    public async Task IsEol_WhenCurrentIsLf_ReturnsTrue()
    {
        using var context = CreateContext("\n", Encoding.UTF8);
        await context.MoveNextAsync();

        var isEol = await context.IsEol();

        Assert.True(isEol);
    }

    /// <summary>
    /// CRLF в текущей позиции считается концом строки.
    /// </summary>
    [Fact]
    public async Task IsEol_WhenCurrentStartsCrlf_ReturnsTrue()
    {
        using var context = CreateContext("\r\n", Encoding.UTF8);
        await context.MoveNextAsync();

        var isEol = await context.IsEol();

        Assert.True(isEol);
    }

    /// <summary>
    /// Одиночный CR без последующего LF не считается концом строки.
    /// </summary>
    [Fact]
    public async Task IsEol_WhenCurrentIsCrWithoutLf_ReturnsFalse()
    {
        using var context = CreateContext("\ra", Encoding.UTF8);
        await context.MoveNextAsync();

        var isEol = await context.IsEol();

        Assert.False(isEol);
    }

    /// <summary>
    /// Повторный MoveNextAsync после достижения конца файла не меняет состояние контекста.
    /// </summary>
    [Fact]
    public async Task MoveNextAsync_WhenEndAlreadyReached_DoesNothing()
    {
        using var context = CreateContext("a", Encoding.UTF8);
        Assert.True(await context.MoveNextAsync());
        Assert.False(await context.MoveNextAsync());
        var byteOffset = context.ByteOffset;

        var moved = await context.MoveNextAsync();

        Assert.False(moved);
        Assert.True(context.IsEof);
        Assert.Equal(byteOffset, context.ByteOffset);
    }

    /// <summary>
    /// IsExprEnd возвращает true, если контекст находится в EOF.
    /// </summary>
    [Fact]
    public async Task IsExprEnd_WhenIsEof_ReturnsTrue()
    {
        using var context = CreateContext(string.Empty, Encoding.UTF8);
        await context.MoveNextAsync();

        var isExprEnd = await context.IsExprEnd;

        Assert.True(isExprEnd);
    }

    /// <summary>
    /// IsExprEnd возвращает true, если текущая позиция является концом строки.
    /// </summary>
    [Fact]
    public async Task IsExprEnd_WhenCurrentIsEol_ReturnsTrue()
    {
        using var context = CreateContext("\n", Encoding.UTF8);
        await context.MoveNextAsync();

        var isExprEnd = await context.IsExprEnd;

        Assert.True(isExprEnd);
    }

    /// <summary>
    /// IsExprEnd возвращает false для обычного символа, если это не EOF и не конец строки.
    /// </summary>
    [Fact]
    public async Task IsExprEnd_WhenCurrentIsNotEolOrEof_ReturnsFalse()
    {
        using var context = CreateContext("a", Encoding.UTF8);
        await context.MoveNextAsync();

        var isExprEnd = await context.IsExprEnd;

        Assert.False(isExprEnd);
    }

    /// <summary>
    /// CurrentByteLength соответствует байтовой длине текущего символа в исходной кодировке.
    /// </summary>
    [Fact]
    public async Task CurrentByteLength_WhenCurrentIsMultibyteSymbol_ReturnsEncodedByteCount()
    {
        const string text = "Ё";
        using var context = CreateContext(text, Encoding.UTF8);
        await context.MoveNextAsync();

        Assert.Equal(Encoding.UTF8.GetByteCount(text), context.CurrentByteLength);
    }

    /// <summary>
    /// После полного прочтения файла ByteOffset равен байтовой длине stream.
    /// </summary>
    [Fact]
    public async Task ByteOffset_WhenWholeFileRead_EqualsFileLength()
    {
        const string text = "abcЁ";
        var bytes = Encoding.UTF8.GetBytes(text);
        using var context = CreateContext(text, Encoding.UTF8);

        await ReadAllAsync(context);

        Assert.Equal(bytes.Length, context.ByteOffset);
    }

    /// <summary>
    /// PeekCharAtAsync с отрицательным offset завершается ошибкой.
    /// </summary>
    [Fact]
    public async Task PeekCharAtAsync_WhenOffsetIsLessThanZero_ThrowsArgumentOutOfRangeException()
    {
        using var context = CreateContext("abc", Encoding.UTF8);
        await context.MoveNextAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => context.PeekCharAtAsync(-1));
    }

    /// <summary>
    /// PeekCharAtAsync с нулевым offset завершается ошибкой.
    /// </summary>
    [Fact]
    public async Task PeekCharAtAsync_WhenOffsetIsZero_ThrowsArgumentOutOfRangeException()
    {
        using var context = CreateContext("abc", Encoding.UTF8);
        await context.MoveNextAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => context.PeekCharAtAsync(0));
    }

    /// <summary>
    /// PeekCharAtAsync за пределами файла возвращает null.
    /// </summary>
    [Fact]
    public async Task PeekCharAtAsync_WhenOffsetIsOutsideFile_ReturnsNull()
    {
        using var context = CreateContext("a", Encoding.UTF8);
        await context.MoveNextAsync();

        var peek = await context.PeekCharAtAsync(1);

        Assert.Null(peek);
    }

    /// <summary>
    /// PeekCharAtAsync с offset больше допустимого лимита завершается ошибкой.
    /// </summary>
    [Fact]
    public async Task PeekCharAtAsync_WhenOffsetExceedsLimit_ThrowsArgumentOutOfRangeException()
    {
        using var context = CreateContext("abc", Encoding.UTF8);
        await context.MoveNextAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.PeekCharAtAsync(TokenizationContext.MaxPeekLength + 1));
    }

    /// <summary>
    /// PeekCharAtAsync на границе текущего буфера выполняет дополнительную буферизацию.
    /// </summary>
    [Fact]
    public async Task PeekCharAtAsync_WhenPeekCrossesBufferBoundary_BuffersAdditionalData()
    {
        var text = new string('a', TokenizationContext.ByteBufferLength + 2);
        var bytes = Encoding.UTF8.GetBytes(text);
        await using var stream = new CountingReadStream(bytes);
        using var context = CreateContext(stream, Encoding.UTF8);

        for (var i = 0; i < TokenizationContext.ByteBufferLength - 2; i++)
        {
            Assert.True(await context.MoveNextAsync());
        }

        var peek = await context.PeekCharAtAsync(TokenizationContext.MaxPeekLength);

        Assert.Equal('a', peek);
        Assert.True(stream.ReadCallCount > 1);
    }

    /// <summary>
    /// После Dispose публичный API контекста становится недоступным.
    /// </summary>
    [Fact]
    public async Task PublicApi_WhenDisposed_ThrowsObjectDisposedException()
    {
        using var stream = new MemoryStream("abc"u8.ToArray());
        var context = CreateContext(stream, Encoding.UTF8);
        context.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = context.Tokens);
        Assert.Throws<ObjectDisposedException>(() => _ = context.Exceptions);
        Assert.Throws<ObjectDisposedException>(() => _ = context.FileName);
        Assert.Throws<ObjectDisposedException>(() => _ = context.ByteOffset);
        Assert.Throws<ObjectDisposedException>(() => _ = context.Encoding);
        Assert.Throws<ObjectDisposedException>(() => _ = context.IsEof);
        Assert.Throws<ObjectDisposedException>(() => _ = context.IsExprEnd);
        Assert.Throws<ObjectDisposedException>(() => _ = context.Current);
        Assert.Throws<ObjectDisposedException>(() => _ = context.CurrentByteLength);
        Assert.Throws<ObjectDisposedException>(() => context.MoveNextAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await context.IsEol());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => context.PeekCharAtAsync(1));
    }

    /// <summary>
    /// Dispose контекста не закрывает исходный stream.
    /// </summary>
    [Fact]
    public void Dispose_DoesNotCloseSourceStream()
    {
        using var stream = new MemoryStream("abc"u8.ToArray());
        var context = CreateContext(stream, Encoding.UTF8);

        context.Dispose();

        Assert.True(stream.CanRead);
        stream.Position = 0;
        Assert.Equal('a', stream.ReadByte());
    }

    private static TokenizationContext CreateContext(string text, Encoding encoding)
    {
        return CreateContext(new MemoryStream(encoding.GetBytes(text)), encoding);
    }

    private static TokenizationContext CreateContext(Stream stream, Encoding encoding)
    {
        return new TokenizationContext(
            [],
            [],
            FileName,
            stream,
            encoding);
    }

    private static async Task<string> ReadAllAsync(TokenizationContext context)
    {
        var builder = new StringBuilder();
        while (await context.MoveNextAsync())
        {
            builder.Append(context.Current);
        }

        return builder.ToString();
    }

    private sealed class UnreadableStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class CountingReadStream(byte[] buffer) : MemoryStream(buffer)
    {
        public int ReadCallCount { get; private set; }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            ReadCallCount++;
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }
    }
}
