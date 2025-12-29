using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class OrdersController : Controller
{
    public IActionResult CreateOrder()
    {
        return View();
    }
}
