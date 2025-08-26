using EncompassAPI.Models.Encompass;
using LiteDB;
using System.Security.Cryptography;

namespace EncompassAPI.Repos
{
    public class TokenRepo : ITokenRepo, IDisposable
    {
        private const string DBPath = "Token.db";
        private const string SObjectCollectionName = "EncompassTokens";
        private static readonly object _dbLock = new();

        private readonly LiteDatabase _db;

        public TokenRepo()
        {
            _db = new LiteDatabase(DBPath);
        }

        public void Upsert(EncompassTokenInfo encompassTokenInfo)
        {
            lock (_dbLock)
            {
                var col = _db.GetCollection<EncompassTokenInfo>(SObjectCollectionName);
                col.Upsert(encompassTokenInfo);
            }
        }

        public EncompassTokenInfo[] Get(string orgId)
        {
            var col = _db.GetCollection<EncompassTokenInfo>(SObjectCollectionName);
            // Find record
            var record = col.Find(x => x.Org_Id == orgId);
            return record.ToArray();
        }


        public void Dispose()
        {
            _db.Dispose();
        }
    }

    // This is the repository interface for encompass token management.
    public interface ITokenRepo
    {
        void Upsert(EncompassTokenInfo sobjectDetail);
        EncompassTokenInfo[] Get(string orgId);
    }
}
