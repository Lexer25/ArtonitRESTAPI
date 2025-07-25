using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Model;
using Microsoft.AspNetCore.Mvc;
using System;

namespace ArtonitRESTAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">id сотрудника</param>
        /// <param name="fromDate">Дата и время начала периода отсчета</param>
        /// <param name="toDate">Дата и время окончания периода отсчета</param>
        /// <param name="deviceGroupName">Название группы устройств</param>
        /// <returns></returns>
        [HttpGet("passIdDg")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetPassIdDg([FromQuery] string id, [FromQuery] string fromDate, [FromQuery] string toDate, [FromQuery] string deviceGroupName)
        {
            try
            {
                var result = EventDBController.GetPassIdDg(id, fromDate, toDate, deviceGroupName);
                return Ok(new { context = result.Value });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">id сотрудника</param>
        /// <param name="fromDate">Дата и время начала периода отсчета</param>
        /// <param name="toDate">Дата и время окончания периода отсчета</param>
        /// <param name="accessPointName">Название группы устройств</param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("passIdAp")]
        public IActionResult GetPassIdAp([FromQuery] string id, [FromQuery] string fromDate, [FromQuery] string toDate, [FromQuery] string accessPointName)
        {
            try
            {
                var result = EventDBController.GetPassIdAp(id, fromDate, toDate, accessPointName);
                return Ok(new { context = result.Value });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("passAllDG")]
        public IActionResult GetPassAllDG([FromQuery] string fromDate, [FromQuery] string toDate, [FromQuery] string deviceGroupName)
        {
            try
            {
                var result = EventDBController.GetPassAllDG(fromDate, toDate, deviceGroupName);
                return Ok(new { context = result.Value });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("passAllAp")]
        public IActionResult GetPassAllAp([FromQuery] string fromDate, [FromQuery] string toDate, [FromQuery] string accessPointName)
        {
            try
            {
                var result = EventDBController.GetPassAllAp(fromDate, toDate, accessPointName);
                return Ok(new { context = result.Value });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}