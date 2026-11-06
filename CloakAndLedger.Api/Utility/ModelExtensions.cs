namespace CloakAndLedger.Api.Utility
{
    public static class ModelExtensions
    {
        public static bool HasTrueProperty<T>(this T model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            return model.GetType()
                        .GetProperties()
                        .Where(p => p.PropertyType == typeof(bool))
                        .Any(p => (bool)p.GetValue(model)!);
        }
    }
}

