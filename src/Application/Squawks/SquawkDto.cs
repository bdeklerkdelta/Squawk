using Squawker.Domain.Entities;

namespace Squawker.Application.Squawks;

public class SquawkDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Squawk, SquawkDto>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToUniversalTime()));
        }
    }
}
