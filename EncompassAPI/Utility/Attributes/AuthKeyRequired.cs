namespace EncompassAPI.Utility.Attributes
{
    /// <summary>
    /// Requires Key to be passed in via header
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AuthKeyRequired : Attribute
    {
    }
}
