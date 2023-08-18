using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bulky.Models.ViewModels
{
	public class UserVM
	{
		public string? Id { get; set; }
		public string? Name { get; set; }
		public string? Role { get; set; }
		[ValidateNever]
		public IEnumerable<SelectListItem> RoleList { get; set; }
		public int? CompanyId { get; set; }
		[ValidateNever]
		public IEnumerable<SelectListItem> CompanyList { get; set; }
	}
}
