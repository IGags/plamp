namespace plamp.Intrinsics;

public static class ReadIntrinsics
{
    public static char Read()     => (char)Console.Read();
    public static string Readln() => Console.ReadLine() ?? "";
}