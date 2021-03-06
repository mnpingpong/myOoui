﻿using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using TabNoc.PiWeb.DataTypes.WateringWeb.History;
using TabNoc.PiWeb.WateringWebServer.other;

namespace TabNoc.PiWeb.WateringWebServer.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class HistoryController : ControllerBase
	{
		public HistoryController()
		{
		}

		public static void AddLogEntry(HistoryElement element)
		{
			using (ConnectionPool.ConnectionUsable usable = new ConnectionPool.ConnectionUsable())
			{
				using (NpgsqlCommand command = usable.Connection.CreateCommand())
				{
					command.CommandText = $"INSERT INTO t_history(msgtimestamp, source, status, message) Values (@time, '{element.Source}', '{element.Status}', '{element.Message}');";
					command.Parameters.AddWithValue("@time", NpgsqlDbType.Timestamp, element.TimeStamp);
					command.ExecuteNonQuery();
				}
			}
		}

		[HttpPost]
		public ActionResult CreateNewEntry(HistoryElement element)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}
			using (ConnectionPool.ConnectionUsable usable = new ConnectionPool.ConnectionUsable())
			{
				using (NpgsqlCommand command = usable.Connection.CreateCommand())
				{
					command.CommandText = $"INSERT INTO t_history(msgtimestamp, source, status, message) Values (@time, @source, @status, @message);";
					command.Parameters.AddWithValue("@time", NpgsqlDbType.Timestamp, element.TimeStamp);
					command.Parameters.AddWithValue("@source", NpgsqlDbType.Text, element.Source);
					command.Parameters.AddWithValue("@status", NpgsqlDbType.Text, element.Status);
					command.Parameters.AddWithValue("@message", NpgsqlDbType.Text, element.Message);
					command.ExecuteNonQuery();
				}
			}

			return Ok();
		}

		/// <summary>
		/// Fragt alle Verlaufsdaten des Servers ohne beschränkung der Menge oder Filterung ab.
		/// </summary>
		/// <returns>Gibt ein <see cref="HistoryData"/> Objekt zurück welches für die Serialisierung verwedet wird.</returns>
		[HttpGet]
		public ActionResult<HistoryData> Get()
		{
			HistoryData data = new HistoryData
			{
				Valid = true
			};

			using (ConnectionPool.ConnectionUsable usable = new ConnectionPool.ConnectionUsable())
			{
				using (NpgsqlCommand command = usable.Connection.CreateCommand())
				{
					command.CommandText = "select * from t_history order by msgtimestamp desc;";
					using (NpgsqlDataReader dataReader = command.ExecuteReader())
					{
						while (dataReader.Read())
						{
							data.HistoryElements.Add(new HistoryElement((DateTime)dataReader[0], (string)dataReader[1], (string)dataReader[2], (string)dataReader[3]));
						}
					}
				}

				return Ok(data);
			}
		}

		/// <summary>
		/// Ruft ein bestimmten Eintrag mittels des PrimaryKeys ab
		/// </summary>
		/// <param name="primaryKey"></param>
		/// <returns></returns>
		[HttpGet("{primaryKey}")]
		public ActionResult<HistoryElement> Get(DateTime primaryKey)
		{
			using (ConnectionPool.ConnectionUsable usable = new ConnectionPool.ConnectionUsable())
			{
				using (NpgsqlCommand command = usable.Connection.CreateCommand())
				{
					command.CommandText = "select * from t_history where msgtimestamp == @primaryKey;";
					command.Parameters.AddWithValue("@primaryKey", NpgsqlDbType.Timestamp, primaryKey);
					using (NpgsqlDataReader dataReader = command.ExecuteReader())
					{
						while (dataReader.Read())
						{
							return Ok(new HistoryElement((DateTime)dataReader[0], (string)dataReader[1], (string)dataReader[2], (string)dataReader[3]));
						}
					}
				}
			}
			throw new Exception("Dieser Punkt darf nicht erreicht werden!");
		}

		/// <summary>
		/// Fragt die Menge an Verlaufseinträgen ohne Filterung ab .
		/// </summary>
		/// <returns>Anzahl an Elementen</returns>
		[HttpGet("amount")]
		public ActionResult<int> GetAmount()
		{
			using (ConnectionPool.ConnectionUsable usable = new ConnectionPool.ConnectionUsable())
			{
				using (NpgsqlCommand command = usable.Connection.CreateCommand())
				{
					command.CommandText = "select count(*) from t_history;";
					using (NpgsqlDataReader dataReader = command.ExecuteReader())
					{
						while (dataReader.Read())
						{
							return Ok(dataReader[0]);
						}
					}
				}
			}
			throw new Exception("Dieser Punkt darf nicht erreicht werden!");
		}

		[HttpGet("enabled")]
		public ActionResult<bool> GetEnabled()
		{
			return Ok(true);
		}

		[HttpGet("range")]
		public ActionResult<IEnumerable<HistoryElement>> GetRange([FromQuery(Name = "primaryKey")] DateTime primaryKey, [FromQuery(Name = "takeAmount")] int takeAmount)
		{
			List<HistoryElement> returnval = new List<HistoryElement>();
			using (ConnectionPool.ConnectionUsable usable = new ConnectionPool.ConnectionUsable())
			{
				using (NpgsqlCommand command = usable.Connection.CreateCommand())
				{
					if (primaryKey == default(DateTime))
					{
						command.CommandText = "select * from t_history order by msgtimestamp desc limit @amount;";
					}
					else
					{
						command.CommandText = "select * from t_history where msgtimestamp <= @fromTime order by msgtimestamp desc limit @amount;";
						command.Parameters.AddWithValue("@fromTime", NpgsqlDbType.Timestamp, primaryKey);
					}

					command.Parameters.AddWithValue("@amount", NpgsqlDbType.Integer, takeAmount);
					using (NpgsqlDataReader dataReader = command.ExecuteReader())
					{
						while (dataReader.Read())
						{
							returnval.Add(new HistoryElement((DateTime)dataReader[0], (string)dataReader[1], (string)dataReader[2], (string)dataReader[3]));
						}
					}
				}
			}

			return Ok(returnval);
		}

		[HttpGet("search")]
		public ActionResult<IEnumerable<HistoryElement>> GetSearched([FromQuery(Name = "searchString")] string searchString, [FromQuery(Name = "collumn")]int collumn, [FromQuery(Name = "amount")]int amount)
		{
			List<HistoryElement> returnval = new List<HistoryElement>();
			using (ConnectionPool.ConnectionUsable usable = new ConnectionPool.ConnectionUsable())
			{
				using (NpgsqlCommand command = usable.Connection.CreateCommand())
				{
					command.CommandText = "select * from t_history where lower(" + GetCollumnName(collumn) + "::text) like lower('%" + searchString + "%') order by msgtimestamp desc limit @amount;";
					//command.Parameters.AddWithValue("@tableName", GetCollumnName(collumn));
					//command.Parameters.AddWithValue("@searchstring", searchString);
					command.Parameters.AddWithValue("@amount", NpgsqlDbType.Integer, amount);
					using (NpgsqlDataReader dataReader = command.ExecuteReader())
					{
						while (dataReader.Read())
						{
							returnval.Add(new HistoryElement((DateTime)dataReader[0], (string)dataReader[1], (string)dataReader[2], (string)dataReader[3]));
						}
					}
				}
			}
			return Ok(returnval);
		}

		private string GetCollumnName(int collumn)
		{
			switch (collumn)
			{
				case 0:
					return "msgtimestamp";

				case 1:
					return "status";

				case 2:
					return "source";

				case 3:
					return "message";

				default:
					throw new IndexOutOfRangeException();
			}
		}
	}
}
