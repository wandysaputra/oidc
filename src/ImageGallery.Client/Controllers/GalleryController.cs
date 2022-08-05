using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityModel.Client;
using ImageGallery.Client.ViewModels;
using ImageGallery.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace ImageGallery.Client.Controllers {
    [Authorize]
    public class GalleryController : Controller {
        private readonly IHttpClientFactory _httpClientFactory;

        public GalleryController (IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory ??
                throw new ArgumentNullException (nameof (httpClientFactory));
        }

        public async Task<IActionResult> Index () {
            await WriteOutIdentityInformation ();

            var httpClient = _httpClientFactory.CreateClient ("APIClient");

            var request = new HttpRequestMessage (
                HttpMethod.Get,
                "/api/images/");

            var response = await httpClient.SendAsync (
                request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait (false);

            if (response.IsSuccessStatusCode) {
                using (var responseStream = await response.Content.ReadAsStreamAsync ()) {
                    return View (new GalleryIndexViewModel (
                        await JsonSerializer.DeserializeAsync<List<Image>> (responseStream)));
                }
            } else if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden) {
                return RedirectToAction ("AccessDenied", "Authorization");
            }

            throw new Exception ("Problem accessing API");

            // response.EnsureSuccessStatusCode();

            // using (var responseStream = await response.Content.ReadAsStreamAsync())
            // {
            //     return View(new GalleryIndexViewModel(
            //         await JsonSerializer.DeserializeAsync<List<Image>>(responseStream)));
            // }
        }

        public async Task<IActionResult> EditImage (Guid id) {

            var httpClient = _httpClientFactory.CreateClient ("APIClient");

            var request = new HttpRequestMessage (
                HttpMethod.Get,
                $"/api/images/{id}");

            var response = await httpClient.SendAsync (
                request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait (false);

            if (response.IsSuccessStatusCode) {
                using (var responseStream = await response.Content.ReadAsStreamAsync ()) {
                    var deserializedImage = await JsonSerializer.DeserializeAsync<Image> (responseStream);

                    var editImageViewModel = new EditImageViewModel () {
                        Id = deserializedImage.Id,
                        Title = deserializedImage.Title
                    };

                    return View (editImageViewModel);
                }
            } else if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden) {
                return RedirectToAction ("AccessDenied", "Authorization");
            }

            throw new Exception ("Problem accessing API");

            // response.EnsureSuccessStatusCode ();

            // using (var responseStream = await response.Content.ReadAsStreamAsync ()) {
            //     var deserializedImage = await JsonSerializer.DeserializeAsync<Image> (responseStream);

            //     var editImageViewModel = new EditImageViewModel () {
            //         Id = deserializedImage.Id,
            //         Title = deserializedImage.Title
            //     };

            //     return View (editImageViewModel);
            // }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditImage (EditImageViewModel editImageViewModel) {
            if (!ModelState.IsValid) {
                return View ();
            }

            // create an ImageForUpdate instance
            var imageForUpdate = new ImageForUpdate () {
                Title = editImageViewModel.Title
            };

            // serialize it
            var serializedImageForUpdate = JsonSerializer.Serialize (imageForUpdate);

            var httpClient = _httpClientFactory.CreateClient ("APIClient");

            var request = new HttpRequestMessage (
                HttpMethod.Put,
                $"/api/images/{editImageViewModel.Id}");

            request.Content = new StringContent (
                serializedImageForUpdate,
                System.Text.Encoding.Unicode,
                "application/json");

            var response = await httpClient.SendAsync (
                request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait (false);

            if (response.IsSuccessStatusCode) {
                return RedirectToAction ("Index");
            } else if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden) {
                return RedirectToAction ("AccessDenied", "Authorization");
            }

            throw new Exception ("Problem accessing API");

            // response.EnsureSuccessStatusCode ();

            // return RedirectToAction ("Index");
        }

        public async Task<IActionResult> DeleteImage (Guid id) {
            var httpClient = _httpClientFactory.CreateClient ("APIClient");

            var request = new HttpRequestMessage (
                HttpMethod.Delete,
                $"/api/images/{id}");

            var response = await httpClient.SendAsync (
                request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait (false);

            response.EnsureSuccessStatusCode ();

            return RedirectToAction ("Index");
        }

        [Authorize (Roles = "PayingUser")]
        public IActionResult AddImage () {
            return View ();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize (Roles = "PayingUser")]
        public async Task<IActionResult> AddImage (AddImageViewModel addImageViewModel) {
            if (!ModelState.IsValid) {
                return View ();
            }

            // create an ImageForCreation instance
            var imageForCreation = new ImageForCreation () { Title = addImageViewModel.Title };

            // take the first (only) file in the Files list
            var imageFile = addImageViewModel.Files.First ();

            if (imageFile.Length > 0) {
                using (var fileStream = imageFile.OpenReadStream ())
                using (var ms = new MemoryStream ()) {
                    fileStream.CopyTo (ms);
                    imageForCreation.Bytes = ms.ToArray ();
                }
            }

            // serialize it
            var serializedImageForCreation = JsonSerializer.Serialize (imageForCreation);

            var httpClient = _httpClientFactory.CreateClient ("APIClient");

            var request = new HttpRequestMessage (
                HttpMethod.Post,
                $"/api/images");

            request.Content = new StringContent (
                serializedImageForCreation,
                System.Text.Encoding.Unicode,
                "application/json");

            var response = await httpClient.SendAsync (
                request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait (false);

            response.EnsureSuccessStatusCode ();

            return RedirectToAction ("Index");
        }

        public async void Logout () {

            if (User?.Identity.IsAuthenticated ?? false) {
                // sign out from ImageGallery Client
                await HttpContext.SignOutAsync (CookieAuthenticationDefaults.AuthenticationScheme);
                Console.WriteLine ("Signed Out from Image Gallery Client");
            }

            if (!string.IsNullOrWhiteSpace (HttpContext?.User?.Identity?.Name)) {
                 // sign out from OIDC
                await HttpContext.SignOutAsync (OpenIdConnectDefaults.AuthenticationScheme);
                Console.WriteLine ("Signed Out from OIDC");
            }

            string accessToken = await HttpContext.GetTokenAsync (OpenIdConnectParameterNames.AccessToken);
            string refreshToken = await HttpContext.GetTokenAsync (OpenIdConnectParameterNames.RefreshToken);

            var idpClient = _httpClientFactory.CreateClient ("IDPClient");

            var discoveryDocumentResponse = await idpClient.GetDiscoveryDocumentAsync ();
            if (discoveryDocumentResponse.IsError) {
                throw new Exception (discoveryDocumentResponse.Error);
            }

            if (!string.IsNullOrWhiteSpace (accessToken)) {
                var accessTokenRevocationResponse = await idpClient.RevokeTokenAsync (new TokenRevocationRequest {
                    Address = discoveryDocumentResponse.RevocationEndpoint,
                        ClientId = "imagegalleryclient",
                        ClientSecret = "secret",
                        Token = accessToken,
                });

                if (accessTokenRevocationResponse.IsError) {
                    throw new Exception (accessTokenRevocationResponse.Error);
                }
            } else {
                Console.WriteLine ($"Invalid access token : {accessToken}");
            }

            if (!string.IsNullOrWhiteSpace (refreshToken)) {
                var refreshTokenRevocationResponse = await idpClient.RevokeTokenAsync (new TokenRevocationRequest {
                    Address = discoveryDocumentResponse.RevocationEndpoint,
                        ClientId = "imagegalleryclient",
                        ClientSecret = "secret",
                        Token = refreshToken,
                });

                if (refreshTokenRevocationResponse.IsError) {
                    throw new Exception (refreshTokenRevocationResponse.Error);
                }
            } else {
                Console.WriteLine ($"Invalid refresh token : {refreshToken}");
            }
        }

        [Authorize (Policy = "CanOrderFrame")]
        // [Authorize (Roles = "PayingUser, abc, def")] // multiple roles seprated with comma
        public async Task<IActionResult> OrderFrame () {
            var idpClient = _httpClientFactory.CreateClient ("IDPClient");

            var metaDataResponse = await idpClient.GetDiscoveryDocumentAsync ();

            if (metaDataResponse.IsError) {
                throw new Exception ("Problem accessing the discovery point", metaDataResponse.Exception);
            }

            var accessToken = await HttpContext.GetTokenAsync (OpenIdConnectParameterNames.AccessToken);

            var userInfoResponse = await idpClient.GetUserInfoAsync (new UserInfoRequest {
                Address = metaDataResponse.UserInfoEndpoint,
                    Token = accessToken
            });

            Console.WriteLine ($"AccessToken : {accessToken}");

            if (userInfoResponse.IsError) {
                throw new Exception ("Problem accessing the UserInfo endpoint.", userInfoResponse.Exception);
            }

            // write out the userInfoResponse claims
            foreach (var claim in userInfoResponse.Claims) {
                Console.WriteLine ($"Claim type: {claim.Type} - Claim value: {claim.Value}");
            }

            var address = userInfoResponse.Claims.FirstOrDefault (f => f.Type == "address")?.Value;

            return View (new OrderFrameViewModel (address));
        }

        public async Task WriteOutIdentityInformation () {
            // get the saved identity token
            var identityToken = await HttpContext.GetTokenAsync (OpenIdConnectParameterNames.IdToken);

            // write it out
            Console.WriteLine ($"Identity token: {identityToken}");

            // write out the user claims
            foreach (var claim in User.Claims) {
                Console.WriteLine ($"Claim type: {claim.Type} - Claim value: {claim.Value}");
            }

            var idpClient = _httpClientFactory.CreateClient ("IDPClient");

            var metaDataResponse = await idpClient.GetDiscoveryDocumentAsync ();

            if (metaDataResponse.IsError) {
                throw new Exception ("Problem accessing the discovery point", metaDataResponse.Exception);
            }

            var accessToken = await HttpContext.GetTokenAsync (OpenIdConnectParameterNames.AccessToken);

            if (!string.IsNullOrWhiteSpace (accessToken)) {

                var userInfoResponse = await idpClient.GetUserInfoAsync (new UserInfoRequest {
                    Address = metaDataResponse.UserInfoEndpoint,
                        Token = accessToken
                });

                Console.WriteLine ($"AccessToken : {accessToken}");

                if (userInfoResponse.IsError) {
                    // throw new Exception ("Problem accessing the UserInfo endpoint.", userInfoResponse.Exception);
                    Console.WriteLine ($"Problem accessing the UserInfo endpoint. {userInfoResponse.Exception}");
                } else {
                    // write out the userInfoResponse claims
                    foreach (var claim in userInfoResponse.Claims) {
                        Console.WriteLine ($"Claim type: {claim.Type} - Claim value: {claim.Value}");
                    }
                }
            } else {
                Console.WriteLine ($"Invalid AccessToken :: {accessToken}");
            }
        }
    }
}