using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Namespace
{
	[DataContract]
	public class UnserializableDataContract
	{
		[IgnoreDataMember]
		[JsonProperty]
		public MyUnserializableClass Foo { get; set; }

		[DataMember]
		[JsonIgnore]
		private string FooSerializationHelper
		{
			get => JsonConvert.SerializeObject(Foo);
			set => Foo = JsonConvert.DeserializeObject<MyUnserializableClass>(value);
		}
	}
}
