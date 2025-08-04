namespace SampleAppWithDeadCode.Services;

public class Calculator
{
    // Used method
    public int Add(int a, int b)
    {
        return a + b;
    }

    // DEAD CODE: Never called
    public int Subtract(int a, int b)
    {
        return a - b;
    }

    // DEAD CODE: Never called
    public int Multiply(int a, int b)
    {
        return a * b;
    }

    // DEAD CODE: Never called
    public double Divide(double a, double b)
    {
        if (b == 0)
        {
            throw new DivideByZeroException();
        }
        return a / b;
    }

    // DEAD CODE: Private method never called
    private double CalculateSquareRoot(double value)
    {
        return Math.Sqrt(value);
    }

    // DEAD CODE: Protected virtual method never overridden or called
    protected virtual double CalculateLogarithm(double value)
    {
        return Math.Log(value);
    }
}