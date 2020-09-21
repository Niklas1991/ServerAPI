using AutoMapper;
using ServerAPI;
using ServerAPI.Entities;
using ServerAPI.Models;
using ServerAPI.Models.Response;

namespace ServerAPI.Data
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
           CreateMap<RegisterRequest, Account>().ReverseMap();
        }
    }
}
