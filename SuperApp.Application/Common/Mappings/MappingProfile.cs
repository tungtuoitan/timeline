using AutoMapper;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Mos;

namespace SuperApp.Application.Common.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Note mappings
            CreateMap<Note, NoteResponse>();
            CreateMap<CreateNoteRequest, Note>()
                .ForMember(dest => dest.NoteId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsArchived, opt => opt.MapFrom(src => false));
            
            CreateMap<UpdateNoteRequest, Note>()
                .ForMember(dest => dest.NoteId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.IsArchived, opt => opt.Ignore());
        }
    }
}