using Newtonsoft.Json;

namespace MyProject.Helper {
    public class IgnorePropsResolver : Newtonsoft.Json.Serialization.DefaultContractResolver {
        private readonly HashSet<string> _propsToIgnore;

        public IgnorePropsResolver(IEnumerable<string> propNames) {
            _propsToIgnore = new HashSet<string>(propNames, StringComparer.OrdinalIgnoreCase);
        }

        protected override IList<Newtonsoft.Json.Serialization.JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) {
            var props = base.CreateProperties(type, memberSerialization);
            return props.Where(p => !_propsToIgnore.Contains(p.PropertyName)).ToList();
        }
    }
}
