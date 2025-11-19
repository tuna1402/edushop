using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Repositories;

namespace EduShop.Core.Services;

public class CustomerService
{
    private readonly CustomerRepository _repo;

    public CustomerService(CustomerRepository repo)
    {
        _repo = repo;
    }

    public List<Customer> GetAll() => _repo.GetAll();

    public Customer? Get(long id) => _repo.GetById(id);

    public long Create(Customer customer, UserContext user)
    {
        return _repo.Insert(customer, user.UserName);
    }

    public void Update(Customer customer, UserContext user)
    {
        _repo.Update(customer, user.UserName);
    }

    public void SoftDelete(long id, UserContext user)
    {
        _repo.SoftDelete(id, user.UserName);
    }
}
