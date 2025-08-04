namespace SampleInheritanceApp;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Sample Inheritance App");
        
        // Use concrete implementation
        IShape circle = new Circle(5);
        Console.WriteLine($"Circle area: {circle.CalculateArea()}");
        
        // Use base class reference
        Animal dog = new Dog("Buddy");
        dog.MakeSound();
        
        // Direct usage
        var cat = new Cat("Whiskers");
        cat.MakeSound();
        cat.Purr();
    }
}

// Interface with used and unused methods
public interface IShape
{
    double CalculateArea();
    double CalculatePerimeter(); // Not called directly but implemented
}

public interface IDrawable
{
    void Draw(); // Never implemented or used
}

// Abstract base class
public abstract class Animal
{
    protected string Name { get; }
    
    protected Animal(string name)
    {
        Name = name;
    }
    
    public abstract void MakeSound();
    
    // Virtual method - overridden in some subclasses
    public virtual void Sleep()
    {
        Console.WriteLine($"{Name} is sleeping");
    }
    
    // Unused protected method - should be Medium confidence
    protected void UnusedProtectedMethod()
    {
        Console.WriteLine("This protected method is never called");
    }
}

// Concrete implementations
public class Circle : IShape
{
    private readonly double radius;
    
    public Circle(double radius)
    {
        this.radius = radius;
    }
    
    public double CalculateArea()
    {
        return Math.PI * radius * radius;
    }
    
    public double CalculatePerimeter()
    {
        return 2 * Math.PI * radius;
    }
    
    // Unused private method - should be High confidence
    private double UnusedCalculateDiameter()
    {
        return 2 * radius;
    }
}

public class Dog : Animal
{
    public Dog(string name) : base(name) { }
    
    public override void MakeSound()
    {
        Console.WriteLine($"{Name} says: Woof!");
    }
    
    // Override Sleep
    public override void Sleep()
    {
        Console.WriteLine($"{Name} is sleeping and dreaming of bones");
    }
    
    // Unused public method specific to Dog - should be Low confidence
    public void UnusedFetch()
    {
        Console.WriteLine($"{Name} is fetching");
    }
}

public class Cat : Animal
{
    public Cat(string name) : base(name) { }
    
    public override void MakeSound()
    {
        Console.WriteLine($"{Name} says: Meow!");
    }
    
    // Does not override Sleep, uses base implementation
    
    public void Purr()
    {
        Console.WriteLine($"{Name} is purring");
    }
    
    // Unused private method - should be High confidence
    private void UnusedScratch()
    {
        Console.WriteLine($"{Name} is scratching");
    }
}

// Completely unused abstract class
public abstract class UnusedBaseClass
{
    public abstract void UnusedAbstractMethod();
    
    public virtual void UnusedVirtualMethod()
    {
        Console.WriteLine("Unused virtual");
    }
}

// Static utility class with mixed usage
public static class MathUtilities
{
    public static int Add(int a, int b) => a + b; // Not used
    
    public static int Multiply(int a, int b) => a * b; // Not used
    
    // Private static unused - should be High confidence
    private static int UnusedDivide(int a, int b) => a / b;
}