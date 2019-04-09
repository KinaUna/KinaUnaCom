﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class VaccinationsController : ControllerBase
    {
        private readonly ProgenyDbContext _context;
        private readonly IDataService _dataService;

        public VaccinationsController(ProgenyDbContext context, IDataService dataService)
        {
            _context = context;
            _dataService = dataService;
        }

        // GET api/vaccinations/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public IActionResult Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _dataService.GetProgenyUserAccessForUser(id, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Vaccination> vaccinationsList = _dataService.GetVaccinationsList(id); // await _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == id && v.AccessLevel >= accessLevel).ToListAsync();
                vaccinationsList = vaccinationsList.Where(v => v.AccessLevel >= accessLevel).ToList();
                if (vaccinationsList.Any())
                {
                    return Ok(vaccinationsList);
                }

                return NotFound();
            }

            return Unauthorized();
        }

        // GET api/vaccinations/5
        [HttpGet("{id}")]
        public IActionResult GetVaccinationItem(int id)
        {
            Vaccination result = _dataService.GetVaccination(id); // await _context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        // POST api/vaccinations
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Vaccination value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to add vaccinations for this child.

                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Vaccination vaccinationItem = new Vaccination();
            vaccinationItem.AccessLevel = value.AccessLevel;
            vaccinationItem.Author = value.Author;
            vaccinationItem.Notes = value.Notes;
            vaccinationItem.VaccinationDate = value.VaccinationDate;
            vaccinationItem.ProgenyId = value.ProgenyId;
            vaccinationItem.VaccinationDescription = value.VaccinationDescription;
            vaccinationItem.VaccinationName = value.VaccinationName;

            _context.VaccinationsDb.Add(vaccinationItem);
            await _context.SaveChangesAsync();
            _dataService.SetVaccination(vaccinationItem.VaccinationId);

            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = vaccinationItem.ProgenyId;
            tItem.AccessLevel = vaccinationItem.AccessLevel;
            tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Vaccination;
            tItem.ItemId = vaccinationItem.VaccinationId.ToString();
            UserInfo userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
            tItem.CreatedBy = userinfo.UserId;
            tItem.CreatedTime = DateTime.UtcNow;
            tItem.ProgenyTime = vaccinationItem.VaccinationDate;

            await _context.TimeLineDb.AddAsync(tItem);
            await _context.SaveChangesAsync();
            _dataService.SetTimeLineItem(tItem.TimeLineId);

            return Ok(vaccinationItem);
        }

        // PUT api/vaccinations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Vaccination value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to edit vaccinations for this child.
                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Vaccination vaccinationItem = await _context.VaccinationsDb.SingleOrDefaultAsync(v => v.VaccinationId == id);
            if (vaccinationItem == null)
            {
                return NotFound();
            }

            vaccinationItem.AccessLevel = value.AccessLevel;
            vaccinationItem.Author = value.Author;
            vaccinationItem.Notes = value.Notes;
            vaccinationItem.VaccinationDate = value.VaccinationDate;
            vaccinationItem.ProgenyId = value.ProgenyId;
            vaccinationItem.VaccinationDescription = value.VaccinationDescription;
            vaccinationItem.VaccinationName = value.VaccinationName;

            _context.VaccinationsDb.Update(vaccinationItem);
            await _context.SaveChangesAsync();
            _dataService.SetVaccination(vaccinationItem.VaccinationId);

            TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                t.ItemId == vaccinationItem.VaccinationId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Vaccination);
            if (tItem != null)
            {
                tItem.ProgenyTime = vaccinationItem.VaccinationDate;
                tItem.AccessLevel = vaccinationItem.AccessLevel;
                _context.TimeLineDb.Update(tItem);
                await _context.SaveChangesAsync();
                _dataService.SetTimeLineItem(tItem.TimeLineId);
            }

            return Ok(vaccinationItem);
        }

        // DELETE api/vaccinations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Vaccination vaccinationItem = await _context.VaccinationsDb.SingleOrDefaultAsync(v => v.VaccinationId == id);
            if (vaccinationItem != null)
            {
                // Check if child exists.
                Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == vaccinationItem.ProgenyId);
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (prog != null)
                {
                    // Check if user is allowed to delete vaccinations for this child.
                    if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                    t.ItemId == vaccinationItem.VaccinationId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Vaccination);
                if (tItem != null)
                {
                    _context.TimeLineDb.Remove(tItem);
                    await _context.SaveChangesAsync();
                    _dataService.RemoveTimeLineItem(tItem.TimeLineId, tItem.ItemType, tItem.ProgenyId);
                }

                _context.VaccinationsDb.Remove(vaccinationItem);
                await _context.SaveChangesAsync();
                _dataService.RemoveVaccination(vaccinationItem.VaccinationId, vaccinationItem.ProgenyId);

                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("[action]/{id}")]
        public IActionResult GetVaccinationMobile(int id)
        {
            Vaccination result = _dataService.GetVaccination(id); // await _context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == id);

            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

                if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
                {
                    return Ok(result);
                }

                return Unauthorized();
            }

            return NotFound();
        }
    }
}
