using AutoMapper;
using EncompassAPI.Models;
using EncompassAPI.Models.Docusign;

namespace EncompassAPI
{
    public class MapperProfile: Profile
    {
        public MapperProfile() 
        {
            CreateMap<CandidIncomingRequest, DraftEditRequest>()
                .ForMember(dest => dest.OrganizationId, opt => opt.MapFrom(src => src.OrgId))
                .ForMember(dest => dest.S3Path, opt => opt.MapFrom(src => src.Path));
            CreateMap<Models.Recipient, Models.Docusign.Recipient>();
        }
    }
}
