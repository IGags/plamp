namespace plamp.Intrinsics;

public static class PrintIntrinsics
{
    public static void Print(int value)      => Console.Write(value);
    public static void Print(uint value)     => Console.Write(value);
    public static void Print(long value)     => Console.Write(value);
    public static void Print(ulong value)    => Console.Write(value);
    public static void Print(float value)    => Console.Write(value);
    public static void Print(double value)   => Console.Write(value);
    public static void Print(string value)   => Console.Write(value);
    public static void Print(char value)     => Console.Write(value);
    public static void Print(bool value)     => Console.Write(value);
    public static void Print(object value)   => Console.Write(value);
    
    public static void Println(int value)      => Console.Write(value);
    public static void Println(uint value)     => Console.Write(value);
    public static void Println(long value)     => Console.Write(value);
    public static void Println(float value)    => Console.Write(value);
    public static void Println(double value)   => Console.Write(value);
    public static void Println(ulong value)    => Console.Write(value);
    public static void Println(string value)   => Console.Write(value);
    public static void Println(char value)     => Console.Write(value);
    public static void Println(bool value)     => Console.Write(value);
    public static void Println(object value)   => Console.Write(value);
}