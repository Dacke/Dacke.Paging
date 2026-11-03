using Mapster;
using Paging.Sample.Web.Data.Models;
using Paging.Sample.Web.Dtos;

namespace Paging.Sample.Web.Mappings;

public class MappingConfig : IRegister {
    public void Register(TypeAdapterConfig config) {
        config.NewConfig<Person, PersonDto>()
            .Map(dest => dest.Id, src => src.PersonId);

#if DEBUG
        config.Compile(true); // Force compilation of ALL configurations
#endif
    }
}
