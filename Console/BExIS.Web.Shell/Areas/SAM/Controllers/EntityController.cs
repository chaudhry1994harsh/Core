﻿using BExIS.Modules.Sam.UI.Models;
using BExIS.Security.Services.Objects;
using System.Linq;
using System.Web.Mvc;
using Telerik.Web.Mvc;
using Telerik.Web.Mvc.Extensions;

namespace BExIS.Modules.Sam.UI.Controllers
{
    public class EntityController : Controller
    {
        // GET: Entity
        public ActionResult Index()
        {
            return View();
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult Entities_Select(GridCommand command)
        {
            var entityManager = new EntityManager();

            // Source + Transformation - Data
            var entities = entityManager.Entities;

            // Filtering
            var filtered = entities;
            var total = filtered.Count();

            // Sorting
            var sorted = (IQueryable<GroupGridRowModel>)filtered.Sort(command.SortDescriptors);

            // Paging
            var paged = sorted.Skip((command.Page - 1) * command.PageSize)
                .Take(command.PageSize);

            return View(new GridModel<GroupGridRowModel> { Data = paged.ToList(), Total = total });
        }
    }
}