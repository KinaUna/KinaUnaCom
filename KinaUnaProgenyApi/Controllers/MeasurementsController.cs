using System;
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
    public class MeasurementsController : ControllerBase
    {
        private readonly ProgenyDbContext _context;

        public MeasurementsController(ProgenyDbContext context)
        {
            _context = context;

        }

        // GET api/measurements/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _context.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Measurement> measurementsList = await _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == id && m.AccessLevel >= accessLevel).ToListAsync();
                if (measurementsList.Any())
                {
                    return Ok(measurementsList);
                }
                return NotFound();
            }

            return Unauthorized();
        }

        // GET api/measurements/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMeasurementItem(int id)
        {
            Measurement result = await _context.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(m => m.MeasurementId == id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _context.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        // POST api/measurements
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Measurement value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to add measurements for this child.

                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Measurement measurementItem = new Measurement();
            measurementItem.AccessLevel = value.AccessLevel;
            measurementItem.Author = value.Author;
            measurementItem.Date = value.Date;
            measurementItem.Circumference = value.Circumference;
            measurementItem.ProgenyId = value.ProgenyId;
            measurementItem.EyeColor = value.EyeColor;
            measurementItem.CreatedDate = DateTime.UtcNow;
            measurementItem.HairColor = value.HairColor;
            measurementItem.Height = value.Height;
            measurementItem.Weight = value.Weight;

            _context.MeasurementsDb.Add(measurementItem);
            await _context.SaveChangesAsync();

            return Ok(measurementItem);
        }

        // PUT api/measurement/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Measurement value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to edit measurements for this child.
                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Measurement measurementItem = await _context.MeasurementsDb.SingleOrDefaultAsync(m => m.MeasurementId == id);
            if (measurementItem == null)
            {
                return NotFound();
            }

            measurementItem.AccessLevel = value.AccessLevel;
            measurementItem.Author = value.Author;
            measurementItem.Date = value.Date;
            measurementItem.Circumference = value.Circumference;
            measurementItem.ProgenyId = value.ProgenyId;
            measurementItem.EyeColor = value.EyeColor;
            measurementItem.CreatedDate = DateTime.UtcNow;
            measurementItem.HairColor = value.HairColor;
            measurementItem.Height = value.Height;
            measurementItem.Weight = value.Weight;

            _context.MeasurementsDb.Update(measurementItem);
            await _context.SaveChangesAsync();

            return Ok(measurementItem);
        }

        // DELETE api/measurements/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Measurement measurementItem = await _context.MeasurementsDb.SingleOrDefaultAsync(m => m.MeasurementId == id);
            if (measurementItem != null)
            {
                // Check if child exists.
                Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == measurementItem.ProgenyId);
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (prog != null)
                {
                    // Check if user is allowed to delete measurements for this child.
                    if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }
                _context.MeasurementsDb.Remove(measurementItem);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetMeasurementMobile(int id)
        {
            Measurement result = await _context.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(m => m.MeasurementId == id);

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
