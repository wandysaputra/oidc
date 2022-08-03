using System;
using System.Linq;
using System.Threading.Tasks;
using ImageGallery.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ImageGallery.API.Authorization {
    public class MustOwnImageHandler : AuthorizationHandler<MustOwnImageRequirement> {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IGalleryRepository galleryRepository;

        public MustOwnImageHandler (IHttpContextAccessor httpContextAccessor, IGalleryRepository galleryRepository) {
            this.httpContextAccessor = httpContextAccessor;
            this.galleryRepository = galleryRepository;
        }

        protected override Task HandleRequirementAsync (AuthorizationHandlerContext context, MustOwnImageRequirement requirement) {
            var imageId = httpContextAccessor.HttpContext.GetRouteValue ("id").ToString ();
            if (!Guid.TryParse (imageId, out Guid imageIdAsGuid)) {
                context.Fail ();
                return Task.CompletedTask;
            }

            var ownerId = context.User.Claims.FirstOrDefault (f => f.Type == "sub")?.Value;
            if (!galleryRepository.IsImageOwner (imageIdAsGuid, ownerId)) {
                context.Fail ();
                return Task.CompletedTask;
            }

            context.Succeed (requirement);
            return Task.CompletedTask;
        }
    }
}