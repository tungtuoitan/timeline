using AutoMapper;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;

namespace SuperApp.Application.Common.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Note mappings
            CreateMap<Note, NoteResponse>()
                .ForMember(dest => dest.NoteId, opt => opt.MapFrom(src => src.NoteId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.IsArchived, opt => opt.MapFrom(src => src.IsArchived));
            
            CreateMap<CreateNoteRequest, Note>()
                .ForMember(dest => dest.NoteId, opt => opt.Ignore())
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsArchived, opt => opt.MapFrom(src => false));
            
            CreateMap<UpdateNoteRequest, Note>()
                .ForMember(dest => dest.NoteId, opt => opt.MapFrom(src => src.NoteId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsArchived, opt => opt.Ignore());

            // User mappings
            CreateMap<User, UserResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.Birthday, opt => opt.MapFrom(src => src.Birthday))
                .ForMember(dest => dest.AuthenticationType, opt => opt.MapFrom(src => src.AuthenticationType))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.LastLoginAt, opt => opt.MapFrom(src => src.LastLoginAt))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => src.IsEmailVerified));

            // UserProfile mappings
            CreateMap<UserProfile, UserProfileResponse>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.AppC, opt => opt.MapFrom(src => src.AppC))
                .ForMember(dest => dest.Parents, opt => opt.MapFrom(src => src.Parents))
                .ForMember(dest => dest.Priorities, opt => opt.MapFrom(src => src.Priorities))
                .ForMember(dest => dest.Statuses, opt => opt.MapFrom(src => src.Statuses))
                .ForMember(dest => dest.Types, opt => opt.MapFrom(src => src.Types))
                .ForMember(dest => dest.RepeatTypes, opt => opt.MapFrom(src => src.RepeatTypes))
                .ForMember(dest => dest.IsUpdatedTodays, opt => opt.MapFrom(src => src.IsUpdatedTodays))
                .ForMember(dest => dest.LastUpdated, opt => opt.MapFrom(src => src.UpdatedAt));

            CreateMap<UpdateUserProfileRequest, UserProfile>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.AppC, opt => opt.MapFrom(src => src.AppC))
                .ForMember(dest => dest.Parents, opt => opt.MapFrom(src => src.Parents))
                .ForMember(dest => dest.Priorities, opt => opt.MapFrom(src => src.Priorities))
                .ForMember(dest => dest.Statuses, opt => opt.MapFrom(src => src.Statuses))
                .ForMember(dest => dest.Types, opt => opt.MapFrom(src => src.Types))
                .ForMember(dest => dest.RepeatTypes, opt => opt.MapFrom(src => src.RepeatTypes))
                .ForMember(dest => dest.IsUpdatedTodays, opt => opt.MapFrom(src => src.IsUpdatedTodays))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.Ignore());

            // StandardRegistry mappings
            CreateMap<StandardRegistry, StandardRegistryResponse>()
                .ForMember(dest => dest.Key, opt => opt.MapFrom(src => src.Code))
                .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));
        }
    }
}