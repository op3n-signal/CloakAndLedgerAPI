namespace EncompassAPI.Models.Configs
{
    public class CacheStorageConfig
    {
        public string S3Bucket { get; set; }

        public string S3CachePrefix { get; set; }

        public string S3BucketPrefix { get; set; }
    }
}
