using AutoMapper;
using EncompassAPI.Models;
using EncompassAPI.Models.Docusign;
using EncompassAPI.Models.Docusign.AnchorTags;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Models.Entity;
using EncompassAPI.Services;
using System.Text.Json;

namespace EncompassAPI
{
    public class MapperProfile: Profile
    {
        public MapperProfile() 
        {
            CreateMap<CandidIncomingRequest, DraftEditRequest>()
                .ForMember(dest => dest.OrganizationId, opt => opt.MapFrom(src => src.OrgId))
                .ForMember(dest => dest.S3Path, opt => opt.MapFrom(src => src.Path));
            CreateMap<CandidIncomingRequest, DraftEditRequestV4>()
                .ForMember(dest => dest.OrganizationId, opt => opt.MapFrom(src => src.OrgId))
                .ForMember(dest => dest.S3Path, opt => opt.MapFrom(src => src.Path));
            CreateMap<Models.Recipient, Models.Docusign.Recipient>();
            CreateMap<Models.Recipient, Models.Docusign.AnchorTags.Recipient>();
            CreateMap<Models.Encompass.Document, DocumentSignerMapping>()
                .ForMember(dest => dest.DocumentName, opt => opt.MapFrom(src => src.name));
            CreateMap<DocumentSignerMappingEntity, DocumentSignerMapping>()
            .ForMember(dest => dest.Signers, opt => opt.MapFrom(src => DeserializeSigners(src.Signers)));
        }

        private static List<Signer> DeserializeSigners(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new List<Signer>();
            return JsonSerializer.Deserialize<List<Signer>>(json);
        }
    }
}
