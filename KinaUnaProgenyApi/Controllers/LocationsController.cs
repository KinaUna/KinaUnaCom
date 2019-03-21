﻿using System.Collections.Generic;
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
    public class LocationsController : ControllerBase
    {
        private readonly ProgenyDbContext _context;

        public LocationsController(ProgenyDbContext context)
        {
            _context = context;

        }


        // GET api/locations/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _context.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Location> locationsList = await _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == id && l.AccessLevel >= accessLevel).ToListAsync();
                if (locationsList.Any())
                {
                    return Ok(locationsList);
                }
                return NotFound();
            }

            return Unauthorized();
        }

        // GET api/locations/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLocationItem(int id)
        {
            Location result = await _context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == id);

            if (result == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _context.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        // POST api/timeline
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Location value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to add locations for this child.

                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Location location = new Location();
            location.AccessLevel = value.AccessLevel;
            location.Author = value.Author;
            location.City = value.City;
            location.Country = value.Country;
            location.County = value.County;
            location.Date = value.Date;
            location.DateAdded = value.DateAdded;
            location.District = value.District;
            location.HouseNumber = value.HouseNumber;
            location.Latitude = value.Latitude;
            location.Longitude = value.Longitude;
            location.Name = value.Name;
            location.Notes = value.Notes;
            location.PostalCode = value.PostalCode;
            location.ProgenyId = value.ProgenyId;
            location.State = value.State;
            location.StreetName = value.StreetName;
            location.Tags = value.Tags;

            _context.LocationsDb.Add(location);
            await _context.SaveChangesAsync();

            return Ok(location);
        }

        // PUT api/timeline/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Location value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to add locations for this child.
                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Location location = await _context.LocationsDb.SingleOrDefaultAsync(l => l.LocationId == id);
            if (location == null)
            {
                return NotFound();
            }

            location.AccessLevel = value.AccessLevel;
            location.Author = value.Author;
            location.City = value.City;
            location.Country = value.Country;
            location.County = value.County;
            location.Date = value.Date;
            location.DateAdded = value.DateAdded;
            location.District = value.District;
            location.HouseNumber = value.HouseNumber;
            location.Latitude = value.Latitude;
            location.Longitude = value.Longitude;
            location.Name = value.Name;
            location.Notes = value.Notes;
            location.PostalCode = value.PostalCode;
            location.ProgenyId = value.ProgenyId;
            location.State = value.State;
            location.StreetName = value.StreetName;
            location.Tags = value.Tags;

            _context.LocationsDb.Update(location);
            await _context.SaveChangesAsync();

            return Ok(location);
        }

        // DELETE api/timeline/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Location location = await _context.LocationsDb.SingleOrDefaultAsync(l => l.LocationId == id);
            if (location != null)
            {
                // Check if child exists.
                Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == location.ProgenyId);
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (prog != null)
                {
                    // Check if user is allowed to delete locations for this child.
                    if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                _context.LocationsDb.Remove(location);
                await _context.SaveChangesAsync();
                return NoContent();
            }

            return NotFound();
        }


        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetLocationMobile(int id)
        {
            Location result = await _context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == id);
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
