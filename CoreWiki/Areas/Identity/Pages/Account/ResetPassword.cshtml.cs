﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CoreWiki.Data.EntityFramework.Security;
using CoreWiki.Areas.Identity.Services;

namespace CoreWiki.Areas.Identity.Pages.Account
{
	[AllowAnonymous]
	public class ResetPasswordModel : PageModel
	{
		private readonly UserManager<CoreWikiUser> _userManager;
		private readonly HIBPClient _HIBPClient;

		public ResetPasswordModel(UserManager<CoreWikiUser> userManager, HIBPClient hIBPClient)
		{
			_userManager = userManager;
			_HIBPClient = hIBPClient;
		}

		[BindProperty]
		public InputModel Input { get; set; }

		public class InputModel
		{
			[Required]
			[EmailAddress]
			public string Email { get; set; }

			[Required]
			[StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
			[DataType(DataType.Password)]
			public string Password { get; set; }

			[DataType(DataType.Password)]
			[Display(Name = "Confirm password")]
			[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
			public string ConfirmPassword { get; set; }

			public string Code { get; set; }
		}

		public IActionResult OnGet(string code = null)
		{
			if (code == null)
			{
				return BadRequest("A code must be supplied for password reset.");
			}
			else
			{
				Input = new InputModel
				{
					Code = code
				};
				return Page();
			}
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

			var user = await _userManager.FindByEmailAsync(Input.Email);
			if (user == null)
			{
				// Don't reveal that the user does not exist
				return RedirectToPage("./ResetPasswordConfirmation");
			}

			var passwordCheck = await _HIBPClient.GetHitsPlainAsync(Input.Password);
			if (passwordCheck > 0)
			{
				ModelState.AddModelError(nameof(Input.Password), "This password is known to hackers, and can lead to your account being compromised, please try another password. For more info goto https://haveibeenpwned.com/Passwords");
				return RedirectToPage("./ResetPasswordConfirmation");
			}

			var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
			if (result.Succeeded)
			{
				return RedirectToPage("./ResetPasswordConfirmation");
			}

			foreach (var error in result.Errors)
			{
				ModelState.AddModelError(string.Empty, error.Description);
			}
			return Page();
		}
	}
}
