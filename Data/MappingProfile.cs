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
           CreateMap<AuthenticateResponse, Account>().ReverseMap();
           CreateMap<UpdateRequest, AccountResponse>().ReverseMap();
           CreateMap<UpdateRequest, Account>().ReverseMap();
           CreateMap<AccountResponse, Account>().ReverseMap();
           CreateMap<Account, UserResponse>().ReverseMap();
        }

    }
}
