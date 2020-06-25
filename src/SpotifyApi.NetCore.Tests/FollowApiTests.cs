﻿using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpotifyApi.NetCore.Authorization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SpotifyApi.NetCore.Tests
{
    [TestClass]
    public class FollowApiTests
    {
        /* For new users:: You need to provide the scope "user-follow-read". If you dont have this in your
         * bearer token then it will fail with authentication errors. */

        [TestCategory("Integration")]
        [TestCategory("User")]
        [TestMethod]
        public async Task CheckCurrentUserFollowsArtists_ArtistID_AnyItems()
        {
            // arrange
            var http = new HttpClient();
            IConfiguration testConfig = TestsHelper.GetLocalConfig();
            var accounts = new AccountsService(http, testConfig);

            var api = new FollowApi(http, accounts);

            // act
            var response = await api.CheckCurrentUserFollowsArtists(new string[] { "74ASZWbe4lXaubB36ztrGX" },
                testConfig.GetValue(typeof(string), "SpotifyUserBearerAccessToken").ToString());

            // assert
            Assert.IsTrue(response.Any<bool>());
        }

        [TestCategory("Integration")]
        [TestCategory("User")]
        [TestMethod]
        public async Task CheckCurrentUserFollowsUsers_UserID_AnyItems()
        {
            // arrange
            var http = new HttpClient();
            IConfiguration testConfig = TestsHelper.GetLocalConfig();
            var accounts = new AccountsService(http, testConfig);

            var api = new FollowApi(http, accounts);

            // act
            var response = await api.CheckCurrentUserFollowsUsers(new string[] { "jkdesxdxvu6uetjdnaro2yrfc" },
                testConfig.GetValue(typeof(string), "SpotifyUserBearerAccessToken").ToString());

            // assert
            Assert.IsTrue(response.Any<bool>());
        }

        [TestCategory("Integration")]
        [TestCategory("User")]
        [TestMethod]
        public async Task CheckCurrentUserFollowsPlaylist_PlaylistID_AnyItems()
        {
            // arrange
            var http = new HttpClient();
            IConfiguration testConfig = TestsHelper.GetLocalConfig();
            var accounts = new AccountsService(http, testConfig);

            var api = new FollowApi(http, accounts);

            // act
            var response = await api.CheckUsersFollowPlaylist("3cEYpjA9oz9GiPac4AsH4n",
                new string[] { "jkdesxdxvu6uetjdnaro2yrfc" },
                testConfig.GetValue(typeof(string), "SpotifyUserBearerAccessToken").ToString());

            // assert
            Assert.IsTrue(response.Any<bool>());
        }
    }
}
