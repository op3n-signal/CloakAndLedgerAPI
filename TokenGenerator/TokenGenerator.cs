namespace TokenGenerator
{
    /// <summary>
    /// Generates the jwt token to use this middleware
    /// </summary>
    internal class TokenGenerator
    {
        static void Main(string[] args)
        {
            //0 is salt
            //1 is siging
            //2 is encryption
            //3 is username
            //4 is number of days from today
            if (args.Length < 5)
            {
                Console.WriteLine("usage: salt signingKey encryptionKey username daysToLive");
            }
            else
            {
                var days = int.Parse(args[4]);
                var creds = SecurityKeysBuilder.Build(args[0], args[1], args[2]);
                var t = Security.GenerateUserToken(args[3], TimeSpan.FromDays(days), creds);
                Console.WriteLine("Token");
                Console.WriteLine(t.Token);
                Console.WriteLine("Will expire at");
                Console.WriteLine($"{t.Expires:u}");
            }
        }
    }
}