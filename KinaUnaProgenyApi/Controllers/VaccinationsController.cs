using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
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

        public VaccinationsController(ProgenyDbContext context)
        {
            _context = context;

        }

        // GET api/vaccinations/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _context.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Vaccination> vaccinationsList = await _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == id && v.AccessLevel >= accessLevel).ToListAsync();
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
        public async Task<IActionResult> GetVaccinationItem(int id)
        {
            Vaccination result = await _context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _context.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());
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

                _context.VaccinationsDb.Remove(vaccinationItem);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetVaccinationMobile(int id)
        {
            Vaccination result = await _context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == id);

            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = _context.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                    u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

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
