namespace entrypoint;

class Program
{
    static void Main(string[] args)
    {
        var parser = """
                     def void main(int a)
                         if(a % 2 == 0)
                             std.WriteLine("Чёт!")
                         else
                             std.WriteLine("Нечет")
                     """;
    }
}