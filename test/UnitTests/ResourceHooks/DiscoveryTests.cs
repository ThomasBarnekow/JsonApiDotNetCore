using JsonApiDotNetCore.Models;
using JsonApiDotNetCore.Hooks;
using System.Collections.Generic;
using Xunit;
using JsonApiDotNetCore.Builders;

namespace UnitTests.ResourceHooks
{
    public class DiscoveryTests
    {

        [Fact]
        public void Hook_Discovery()
        {
            // arrange & act
            var hookConfig = new HooksDiscovery<Dummy>();
            // assert
            Assert.Contains(ResourceHook.BeforeDelete, hookConfig.ImplementedHooks);
            Assert.Contains(ResourceHook.AfterDelete, hookConfig.ImplementedHooks);

        }
        public class Dummy : Identifiable { }
        public class DummyResourceDefinition : ResourceDefinition<Dummy>
        {
            public DummyResourceDefinition() : base(new ResourceGraphBuilder().AddResource<Dummy>().Build())
            {
            }

            public override IEnumerable<Dummy> BeforeDelete(HashSet<Dummy> entities, ResourcePipeline pipeline) { return entities; }
            public override void AfterDelete(HashSet<Dummy> entities, ResourcePipeline pipeline, bool succeeded) {  }
        }
    }
}
