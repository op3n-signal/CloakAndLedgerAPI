using System.Runtime.Serialization;

namespace EncompassAPI.Models
{
    [DataContract]
    public class ResponseMessage<ModelType>
    {
        /// <summary>
        /// Response message
        /// </summary>
        [DataMember]
        public List<string> ValidationMessages { get; set; }
        /// <summary>
        /// Response message
        /// </summary>
        [DataMember]
        public ModelType Result { get; set; }
    }
}
