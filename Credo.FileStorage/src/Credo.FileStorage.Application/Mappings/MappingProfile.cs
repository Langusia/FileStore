using AutoMapper;
using Credo.FileStorage.Application.Features.Todo.Commands.CreateTodo;
using Credo.FileStorage.Application.Features.Todo.Queries.GetTodo;
using Credo.FileStorage.Application.Features.Todo.Queries.GetTodoList;
using Credo.FileStorage.Domain.Models;

namespace Credo.FileStorage.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Todo, GetTodoListDto>()
            .ForMember(
                dest => dest.Status,
                opt => opt.MapFrom
                (src => ((TodoStatus?)src.Status).ToString()
                )
            );

        CreateMap<Todo, GetTodoDto>()
            .ForMember(
                dest => dest.Status,
                opt => opt.MapFrom
                (src => ((TodoStatus?)src.Status).ToString()
                )
            );

        CreateMap<CreateTodoCommand, Todo>()
            .ForMember(
                dest => dest.Id,
                opt => opt.MapFrom(src => Guid.NewGuid()
                )
            );
    }
}