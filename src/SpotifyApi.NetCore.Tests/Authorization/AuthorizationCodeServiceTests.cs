using System;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using SpotifyApi.NetCore.Authorization;

namespace SpotifyApi.NetCore.Tests.Authorization
{
    [TestClass]
    public class AuthorizationCodeServiceTests
    {
        const string UserHash = "E11AC28538A7C0A827A726DD9B30B710FC1FCAFFFE2E86FCA853AB90E7C710D2";

        [TestMethod]
        public async Task RequestAuthorizationUrl_UserHash_UrlContainsUserHash()
        {
            // arrange
            var http = new HttpClient();
            var service = new AuthorizationCodeService(http, TestsHelper.GetLocalConfig(), Mocks().Data.Object, null);

            // act
            string url = await service.RequestAuthorizationUrl(UserHash);

            // assert
            Assert.IsTrue(url.Contains(UserHash), "url should contain userHash");
        }

        [TestMethod]
        public async Task RequestAuthorizationUrl_Scopes_UrlContainsSpaceDelimitedScopes()
        {
            // arrange
            string[] scopes = new[]
            {
                "user-modify-playback-state",
                "user-read-playback-state",
                "playlist-read-collaborative",
                "playlist-modify-public",
                "playlist-modify-private",
                "playlist-read-private"
            };

            var http = new HttpClient();
            var service = new AuthorizationCodeService(http, TestsHelper.GetLocalConfig(), Mocks().Data.Object, scopes);

            // act
            string url = await service.RequestAuthorizationUrl(UserHash);

            // assert
            Assert.IsTrue(url.Contains(string.Join(" ", scopes)), "url should contain space delimited user scopes");
            Trace.WriteLine("RequestAuthorizationUrl_Scopes_UrlContainsSpaceDelimitedScopes url =");
            Trace.WriteLine(url);

            // https://accounts.spotify.com/authorize/?client_id=944843be09874db29728e51a0a4e8376&response_type=code&redirect_uri=http://localhost:3978/authorize/spotify&scope=user-modify-playback-state user-read-playback-state playlist-read-collaborative playlist-modify-public playlist-modify-private playlist-read-private&state=E11AC28538A7C0A827A726DD9B30B710FC1FCAFFFE2E86FCA853AB90E7C710D2|e80aa62d1eec4041946386b1fe5ad055
        }

        [TestMethod]
        public async Task RequestAuthorizationUrl_ValidDtoCreated_InsertOrReplaceIsCalled()
        {
            // arrange
            var http = new HttpClient();
            var (mockData, mockUserAuth) = Mocks();
            var service = new AuthorizationCodeService(http, TestsHelper.GetLocalConfig(), mockData.Object, null);

            // act
            string url = await service.RequestAuthorizationUrl(UserHash);

            // assert
            mockData.Verify(d => d.InsertOrReplace(mockUserAuth.Object), Times.Once);
        }

        [TestMethod]
        public async Task RequestAuthorizationUrl_CreatedDtoStateNull_InnerExceptionMessageContainsExpectedValue()
        {
            // arrange
            var http = new HttpClient();
            var (mockData, mockUserAuth) = Mocks();
            // setup the State property to return invalid value
            mockUserAuth.SetupGet(u => u.State).Returns("Not a valid state value");
            var service = new AuthorizationCodeService(http, TestsHelper.GetLocalConfig(), mockData.Object, null);

            // act
            string message = null;
            try
            {
                await service.RequestAuthorizationUrl(UserHash);
            }
            catch (Exception ex)
            {
                message = ex.InnerException.Message;
            }

            // assert
            Assert.IsTrue(message.Contains(mockUserAuth.Object.State), "Inner exception message should contain invalid value");
        }

        [TestMethod]
        public async Task RequestAuthorizationUrl_InvalidDtoCreated_InsertOrReplaceNeverCalled()
        {
            // arrange
            var http = new HttpClient();
            var (mockData, mockUserAuth) = Mocks();
            // setup the State property to return invalid value
            mockUserAuth.SetupGet(u => u.State).Returns("Not a valid state value");

            var service = new AuthorizationCodeService(http, TestsHelper.GetLocalConfig(), mockData.Object, null);

            // act
            try
            {
                string url = await service.RequestAuthorizationUrl(UserHash);
            }
            catch (InvalidOperationException)
            {
                // InvalidOperationException is expected
            }

            // assert
            mockData.Verify(d => d.InsertOrReplace(mockUserAuth.Object), Times.Never);
        }

        [TestMethod]
        public async Task RequestTokens_Code_GetUserAuthTokensCalled()
        {
            // arrange
            const string state = "e80aa62d1eec4041946386b1fe5ad055";
            const string code = "36b3860e-cb5a-4076-8b95-e253657e2021";

            var authCode = new AuthorizationTokens
            {
                accessToken = "",
                authUrl = "",
                tokenType = "",
                scope = "",
                expires = DateTime.Now,
                refreshToken = ""
            };

            var http = new HttpClient();
            var (mockData, mockUserAuth) = Mocks();
            mockUserAuth.SetupGet(u=>u.State).Returns(state);

            var mockService = new Mock<AuthorizationCodeService>(http, TestsHelper.GetLocalConfig(), mockData.Object, null) { CallBase = true };
            mockService.Setup(s => s.GetAuthorizationTokens(It.IsAny<string>())).ReturnsAsync(() => authCode);

            await mockService.Object.RequestTokens(StateHelper.EncodeState((UserHash, state)), code);

            // assert
            mockService.Verify(s=>s.GetAuthorizationTokens(code), Times.Once());
        }

        [TestMethod]
        public async Task RequestTokens_Code_DataUpdateCalled()
        {
            // arrange
            const string state = "e80aa62d1eec4041946386b1fe5ad055";
            const string code = "36b3860e-cb5a-4076-8b95-e253657e2021";

            var authCode = new AuthorizationTokens
            {
                accessToken = "",
                authUrl = "",
                tokenType = "",
                scope = "",
                expires = DateTime.Now,
                refreshToken = ""
            };

            var http = new HttpClient();
            var (mockData, mockUserAuth) = Mocks();
            mockUserAuth.SetupGet(u=>u.State).Returns(state);

            var mockService = new Mock<AuthorizationCodeService>(http, TestsHelper.GetLocalConfig(), mockData.Object, null) { CallBase = true };
            mockService.Setup(s => s.GetAuthorizationTokens(It.IsAny<string>())).ReturnsAsync(() => authCode);

            await mockService.Object.RequestTokens(StateHelper.EncodeState((UserHash, state)), code);

            // assert
            mockUserAuth.Object.SetPropertiesFromAuthCode(authCode);
            mockData.Verify(d => d.Update(mockUserAuth.Object), Times.Exactly(2));
        }

        // http://localhost:3978/authorize/spotify?code=AQCQjxUS0zLXbEybK3sEz8pJKzu7nI5eOQU2oVgZXc1lIBGH_i-MSzIsb2A1BUip37A7xMxuVV596ZE7vRm3awJ6rfWR0hFBmqGp-Euef2WJ5EdndB7ynFLoB45pbU-bMShUC-R9tc-Akm3VyM8omrDFlchfEQPi6JjSJoqrRhd3xiPjxAoMTBLypC0Ouvz2ifxJ9jWRzQo843M-_07wgRZkfksF6TUhHxkVHRCGjtO9JharNJuggQCEH0W8GY5s_qgKtU6hdBwhVTcbntVI9sn_gYvP9nc_Ncv1fdAbWKHwL6OM2djE548C_n3dKmyhAGvCxuES5ZF26Kq50cVWCILfXE3dXS7Y33dW3CmbAYYoSA97t98bzHA1SyH-ZECcwlf0rijNDs6G-BQ5IXjKyUJAYzZ_jBjs&state=E11AC28538A7C0A827A726DD9B30B710FC1FCAFFFE2E86FCA853AB90E7C710D2%7Ce80aa62d1eec4041946386b1fe5ad055

        [TestMethod]
        [TestCategory("Integration")]
        public async Task RequestTokens_RealCode_ReturnsTokens()
        {
            // arrange
            const string userHash = "E11AC28538A7C0A827A726DD9B30B710FC1FCAFFFE2E86FCA853AB90E7C710D2";
            const string state = "e80aa62d1eec4041946386b1fe5ad055";
            const string queryState = "E11AC28538A7C0A827A726DD9B30B710FC1FCAFFFE2E86FCA853AB90E7C710D2|e80aa62d1eec4041946386b1fe5ad055";
            const string code = "AQCQjxUS0zLXbEybK3sEz8pJKzu7nI5eOQU2oVgZXc1lIBGH_i-MSzIsb2A1BUip37A7xMxuVV596ZE7vRm3awJ6rfWR0hFBmqGp-Euef2WJ5EdndB7ynFLoB45pbU-bMShUC-R9tc-Akm3VyM8omrDFlchfEQPi6JjSJoqrRhd3xiPjxAoMTBLypC0Ouvz2ifxJ9jWRzQo843M-_07wgRZkfksF6TUhHxkVHRCGjtO9JharNJuggQCEH0W8GY5s_qgKtU6hdBwhVTcbntVI9sn_gYvP9nc_Ncv1fdAbWKHwL6OM2djE548C_n3dKmyhAGvCxuES5ZF26Kq50cVWCILfXE3dXS7Y33dW3CmbAYYoSA97t98bzHA1SyH-ZECcwlf0rijNDs6G-BQ5IXjKyUJAYzZ_jBjs";

            var http = new HttpClient();
            var (mockData, mockUserAuth) = Mocks();
            mockUserAuth.SetupGet(u=>u.State).Returns(state);

            var service = new AuthorizationCodeService(http, TestsHelper.GetLocalConfig(), mockData.Object, null);

            var tokens = await service.RequestTokens(userHash, state, queryState, code);
            Trace.WriteLine("RequestTokens_RealCode_ReturnsTokens tokens = ");
            Trace.WriteLine(tokens);
        }


        private (Mock<IUserAuthData> Data, Mock<IUserAuthEntity> UserAuth) Mocks()
        {
            var mockUserAuth = new Mock<IUserAuthEntity>();
            mockUserAuth.SetupAllProperties();
            mockUserAuth.SetupGet(u => u.UserHash).Returns(UserHash);

            var setState = new Func<string, string, IUserAuthEntity>((h, s) =>
           {
               mockUserAuth.Object.State = s;
               return mockUserAuth.Object;
           });

            var mockData = new Mock<IUserAuthData>();
            mockData.Setup(d => d.Create(It.IsAny<string>(), It.IsAny<string>())).Returns(setState);
            mockData.Setup(d => d.Get(It.IsAny<string>())).ReturnsAsync(mockUserAuth.Object);
            return (mockData, mockUserAuth);
        }
    }
}