﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shopping.Common;
using Shopping.Data;
using Shopping.Data.Entities;
using Shopping.Enums;
using Shopping.Helpers;
using Shopping.Models;

namespace Shopping.Controllers
{
	[Authorize(Roles = "Admin")]
	public class UsersController : Controller
	{
		private readonly IUserHelper _userHelper;
		private readonly DataContext _context;
		private readonly ICombosHelper _combosHelper;
		private readonly IImageHelper _imageHelper;
		private readonly IMailHelper _mailHelper;

		public UsersController(IUserHelper userHelper, DataContext context, ICombosHelper combosHelper, IImageHelper imageHelper, IMailHelper mailHelper )
		{
			_userHelper = userHelper;
			_context = context;
			_combosHelper = combosHelper;
			_imageHelper = imageHelper;
			_mailHelper = mailHelper;
		}

		public async Task<IActionResult> Index()
		{
			return View(await _context.Users
				.Include(u => u.City)
				.ThenInclude(c => c.State)
				.ThenInclude(s => s.Country)
				.ToListAsync());
		}

		public async Task<IActionResult> Create()
		{
			AddUserViewModel model = new AddUserViewModel
			{
				Id = Guid.Empty.ToString(),
				Countries = await _combosHelper.GetComboCountriesAsync(),
				States = await _combosHelper.GetComboStatesAsync(0),
				Cities = await _combosHelper.GetComboCitiesAsync(0),
				UserType = UserType.Admin,
			};

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(AddUserViewModel model)
		{
			if (ModelState.IsValid)
			{
				string path = string.Empty;

				if (model.ImageFile != null)
				{
					path = await _imageHelper.UploadImageAsync(model.ImageFile, "Users");
				}

				User user = await _userHelper.AddUserAsync(model, path);
				if (user == null)
				{
					ModelState.AddModelError(string.Empty, "Este correo ya está siendo usado.");
					return View(model);
				}

                string myToken = await _userHelper.GenerateEmailConfirmationTokenAsync(user);
                string tokenLink = Url.Action("ConfirmEmail", "Account", new
                {
                    userid = user.Id,
                    token = myToken
                }, protocol: HttpContext.Request.Scheme);

                Response response = _mailHelper.SendMail(
                    $"{model.FirstName} {model.LastName}",
                    model.Username,
                    "Shopping - Confirmación de Email",
                    $"<h1>Shopping - Confirmación de Email</h1>" +
                        $"Para habilitar el usuario por favor hacer clicn en el siguiente link:, " +
                        $"<p><a href = \"{tokenLink}\">Confirmar Email</a></p>");
                if (response.IsSuccess)
                {
                    ViewBag.Message = "Las instrucciones para habilitar el usuario han sido enviadas al correo.";
                    return View(model);
                }

                ModelState.AddModelError(string.Empty, response.Message);

            }

            return View(model);
		}


		public JsonResult? GetStates(int countryId)
		{
			Country? country = _context.Countries
				.Include(c => c.States)
				.FirstOrDefault(c => c.Id == countryId);
			if (country == null)
			{
				return null;
			}

			return Json(country.States.OrderBy(d => d.Name));
		}

		public JsonResult? GetCities(int stateId)
		{
			State? state = _context.States
				.Include(s => s.Cities)
				.FirstOrDefault(s => s.Id == stateId);
			if (state == null)
			{
				return null;
			}

			return Json(state.Cities.OrderBy(c => c.Name));
		}

		public IActionResult Login()
		{
			if (User.Identity.IsAuthenticated)
			{
				return RedirectToAction("Index", "Home");
			}

			return View(new LoginViewModel());
		}
	}

}
