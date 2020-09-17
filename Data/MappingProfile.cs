using AutoMapper;
using ServerAPI;
using ServerAPI.Entities;
using ServerAPI.Models;

namespace ServerAPI.Data
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
           // CreateMap<RegisterRequest, Account>().ReverseMap();
        }
    }
}
