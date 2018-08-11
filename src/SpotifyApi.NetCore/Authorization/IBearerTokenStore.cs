using System;
using System.Threading.Tasks;

namespace SpotifyApi.NetCore
{
    public interface IBearerTokenStore
    {   
        Task InsertOrReplace(string key, BearerAccessToken token);
        
        Task<BearerAccessToken> Get(string key);
    }

    //TODO: -> IRefreshTokenProvider?
    public interface IRefreshTokenStore
    {   
        [Obsolete]  //TODO: Up to the consumer how they store refresh tokens returned by the service
        Task InsertOrReplace(string userHash, string token);
        
        //TODO -> GetRefreshToken
        Task<string> Get(string userHash);
    }
}