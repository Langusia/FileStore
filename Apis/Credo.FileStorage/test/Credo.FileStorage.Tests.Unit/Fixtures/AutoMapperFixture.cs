using AutoMapper;
using Credo.FileStorage.Application.Mappings;

namespace Credo.FileStorage.Tests.Unit.Fixtures;

public class AutoMapperFixture
{
    public IMapper Mapper { get; }

    public AutoMapperFixture()
    {
        var mapperConfig = new MapperConfiguration(c => { c.AddProfile<MappingProfile>(); });

        Mapper = mapperConfig.CreateMapper();
    }
}