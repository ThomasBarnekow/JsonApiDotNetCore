using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using JsonApiDotNetCore.Serialization.Objects;
using TestBuildingBlocks;
using Xunit;

namespace JsonApiDotNetCoreTests.IntegrationTests.HostingInIIS
{
    public sealed class HostingTests : IClassFixture<IntegrationTestContext<HostingStartup<HostingDbContext>, HostingDbContext>>
    {
        private const string HostPrefix = "http://localhost";

        private readonly IntegrationTestContext<HostingStartup<HostingDbContext>, HostingDbContext> _testContext;
        private readonly HostingFakers _fakers = new();

        public HostingTests(IntegrationTestContext<HostingStartup<HostingDbContext>, HostingDbContext> testContext)
        {
            _testContext = testContext;

            testContext.UseController<PaintingsController>();
            testContext.UseController<ArtGalleriesController>();
        }

        [Fact]
        public async Task Get_primary_resources_with_include_returns_links()
        {
            // Arrange
            ArtGallery gallery = _fakers.ArtGallery.Generate();
            gallery.Paintings = _fakers.Painting.Generate(1).ToHashSet();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                await dbContext.ClearTableAsync<ArtGallery>();
                dbContext.ArtGalleries.Add(gallery);
                await dbContext.SaveChangesAsync();
            });

            const string route = "/iis-application-virtual-directory/public-api/artGalleries?include=paintings";

            // Act
            (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.OK);

            responseDocument.Links.ShouldNotBeNull();
            responseDocument.Links.Self.Should().Be($"{HostPrefix}{route}");
            responseDocument.Links.Related.Should().BeNull();
            responseDocument.Links.First.Should().Be(responseDocument.Links.Self);
            responseDocument.Links.Last.Should().Be(responseDocument.Links.Self);
            responseDocument.Links.Prev.Should().BeNull();
            responseDocument.Links.Next.Should().BeNull();

            responseDocument.Data.ManyValue.ShouldHaveCount(1);

            responseDocument.Data.ManyValue[0].With(resource =>
            {
                string galleryLink = $"{HostPrefix}/iis-application-virtual-directory/public-api/artGalleries/{gallery.StringId}";

                resource.Links.ShouldNotBeNull();
                resource.Links.Self.Should().Be(galleryLink);

                resource.Relationships.ShouldContainKey("paintings").With(value =>
                {
                    value.ShouldNotBeNull();
                    value.Links.ShouldNotBeNull();
                    value.Links.Self.Should().Be($"{galleryLink}/relationships/paintings");
                    value.Links.Related.Should().Be($"{galleryLink}/paintings");
                });
            });

            string paintingLink =
                $"{HostPrefix}/iis-application-virtual-directory/custom/path/to/paintings-of-the-world/{gallery.Paintings.ElementAt(0).StringId}";

            responseDocument.Included.ShouldHaveCount(1);

            responseDocument.Included[0].With(resource =>
            {
                resource.Links.ShouldNotBeNull();
                resource.Links.Self.Should().Be(paintingLink);

                resource.Relationships.ShouldContainKey("exposedAt").With(value =>
                {
                    value.ShouldNotBeNull();
                    value.Links.ShouldNotBeNull();
                    value.Links.Self.Should().Be($"{paintingLink}/relationships/exposedAt");
                    value.Links.Related.Should().Be($"{paintingLink}/exposedAt");
                });
            });
        }

        [Fact]
        public async Task Get_primary_resources_with_include_on_custom_route_returns_links()
        {
            // Arrange
            Painting painting = _fakers.Painting.Generate();
            painting.ExposedAt = _fakers.ArtGallery.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                await dbContext.ClearTableAsync<Painting>();
                dbContext.Paintings.Add(painting);
                await dbContext.SaveChangesAsync();
            });

            const string route = "/iis-application-virtual-directory/custom/path/to/paintings-of-the-world?include=exposedAt";

            // Act
            (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.OK);

            responseDocument.Links.ShouldNotBeNull();
            responseDocument.Links.Self.Should().Be($"{HostPrefix}{route}");
            responseDocument.Links.Related.Should().BeNull();
            responseDocument.Links.First.Should().Be(responseDocument.Links.Self);
            responseDocument.Links.Last.Should().Be(responseDocument.Links.Self);
            responseDocument.Links.Prev.Should().BeNull();
            responseDocument.Links.Next.Should().BeNull();

            responseDocument.Data.ManyValue.ShouldHaveCount(1);

            responseDocument.Data.ManyValue[0].With(resource =>
            {
                string paintingLink = $"{HostPrefix}/iis-application-virtual-directory/custom/path/to/paintings-of-the-world/{painting.StringId}";

                resource.Links.ShouldNotBeNull();
                resource.Links.Self.Should().Be(paintingLink);

                resource.Relationships.ShouldContainKey("exposedAt").With(value =>
                {
                    value.ShouldNotBeNull();
                    value.Links.ShouldNotBeNull();
                    value.Links.Self.Should().Be($"{paintingLink}/relationships/exposedAt");
                    value.Links.Related.Should().Be($"{paintingLink}/exposedAt");
                });
            });

            responseDocument.Included.ShouldHaveCount(1);

            responseDocument.Included[0].With(resource =>
            {
                string galleryLink = $"{HostPrefix}/iis-application-virtual-directory/public-api/artGalleries/{painting.ExposedAt.StringId}";

                resource.Links.ShouldNotBeNull();
                resource.Links.Self.Should().Be(galleryLink);

                resource.Relationships.ShouldContainKey("paintings").With(value =>
                {
                    value.ShouldNotBeNull();
                    value.Links.ShouldNotBeNull();
                    value.Links.Self.Should().Be($"{galleryLink}/relationships/paintings");
                    value.Links.Related.Should().Be($"{galleryLink}/paintings");
                });
            });
        }
    }
}
