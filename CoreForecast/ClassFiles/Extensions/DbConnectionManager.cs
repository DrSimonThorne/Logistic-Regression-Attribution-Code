using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CoreForecast.ClassFiles;
using System.Threading;

namespace CoreForecast.ClassFiles.Extensions
{
    public class DbConnectionManager
    {
        private MySqlConnection connection;
        private string server;
        private string startDay;
        private string startDate;
        private string endDate;
        public string account;
        private string productId;
        public DbConnectionManager(string inServer)
        {
            server = inServer;
            connection = new MySqlConnection(splitConnectionString(server).ToString());
        }
        public DbConnectionManager(string inServer, string productId)
        {
            this.server = inServer;
            this.productId = productId;
            this.connection = new MySqlConnection(splitConnectionString(server).ToString());
        }
        public DbConnectionManager(string inServer, string inStartDate, string inEndDate, string productId)
        {
            this.server = inServer;
            this.startDate = inStartDate;
            this.endDate = inEndDate;
            this.productId = productId;
            this.connection = new MySqlConnection(splitConnectionString(server).ToString());
        }
        private MySqlConnectionStringBuilder splitConnectionString(string server)
        {
            string[] splitConString = Regex.Split(server, "[^a-zA-Z0-9._-]+");
            MySqlConnectionStringBuilder conn = new MySqlConnectionStringBuilder();
            conn.UserID = splitConString[1];
            conn.Password = splitConString[2];
            conn.Server = splitConString[3];
            conn.Database = splitConString[5];
            conn.SslMode = MySqlSslMode.None;
            this.account = splitConString[5];
            Console.WriteLine($"Connecting to: {splitConString[5].ToUpper()} ({splitConString[3]})");

            return conn;
        }
        //Select statements
        public List<PredictionData> selectSalesData(FrequencyData frequency)
        {
            // Note: This might have problems when it comes to users who have an empty string ( "" ) as a transaction ID,
            // one client to look at would be sainsb
            const string query = @"
	    set group_concat_max_len = 2000000;
	    SELECT
                a.visitor_id AS `visitor`,
                GROUP_CONCAT(b.visit_id ORDER BY b.visit_id) AS `visit_ids`,
                GROUP_CONCAT(c.event_path ORDER BY b.visit_id) AS `events`,
                SUM(d.duration) AS `duration`,
                SUM(e.views) AS `views`,
                COUNT(b.visit_id) AS `visits`,
                MIN(b.first_visit) AS `start`,
                MAX(b.last_visit) AS `end`,
                MAX(b.sale) as `sale`
            FROM agg_sales_temp a
            JOIN agg_sales_temp_journeys b on b.journey_id = a.journey_id
            JOIN agg_event_funnel_visit c on c.visit_id = b.visit_id
            JOIN agg_prediction_lag d on d.visit_id = b.visit_id
            JOIN attrib_visit e on e.id = b.visit_id
            WHERE a.sales_date BETWEEN @startdate AND @enddate
            AND a.product_id = @productid
            GROUP BY a.id
	    ;"
            ;
            //int[] eventssplit = Array.ConvertAll(selectEventsInstantiate().Split(','), int.Parse);
            Dictionary<int, int> eventsTrackedByClient = assignEventFactors();
            List<int> noneSaleEvents = getNoneSaleEvents();
            List<PredictionData> SaleVisits = new List<PredictionData>();
            //Open connection
            if (openConnection() == true)
            {
                Console.WriteLine("\t Pull Sales Visits...");
                //Create Command
                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@startdate", startDate);
                    cmd.Parameters.AddWithValue("@enddate", endDate);
                    cmd.Parameters.AddWithValue("@productid", productId);
                    cmd.CommandTimeout = 0;
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        PredictionData visitor = new PredictionData(
                            eventsTrackedByClient,
                            noneSaleEvents,
                            dataReader.GetInt32("visitor"),
                            dataReader["visit_ids"].ToString(),
                            dataReader["events"].ToString(),
                            dataReader.GetInt32("views"),
                            dataReader.GetInt32("visits"),
                            dataReader.GetInt32("duration"),
                            dataReader.GetDateTime("start"),
                            dataReader.GetDateTime("end"),
                            dataReader.GetBoolean("sale"),
                            frequency
                        );
                        SaleVisits.Add(visitor);
                    }
                    //close Data Reader
                    dataReader.Close();
                }
                //close Connection
                closeConnection();
                //return list to be displayed
                Console.WriteLine("\t\tPulled " + SaleVisits.Count);
                return SaleVisits;
            }
            else
            {
                return SaleVisits;
            }
        }
        public List<PredictionData> selectNoneSalesData(FrequencyData frequency)
        {
            string query = $@"
	    SET group_concat_max_len = 2000000;

	    SELECT
	            a.visitor_id as `visitor`,
	            GROUP_CONCAT(a.id) as `visit_ids`,
	            GROUP_CONCAT(c.event_path) as `events`,
	            SUM(a.views) AS `views`,
	            COUNT(a.id) AS `visits`,
	            SUM(d.duration) AS `duration`,
	            a.first_visit AS `visitStart`,
	            a.last_visit AS `visitEnd`
            FROM attrib_visit a
            LEFT JOIN agg_sales_temp_journeys b on b.visit_id = a.id
            LEFT JOIN agg_event_funnel_visit c on c.visit_id = a.id
            JOIN agg_prediction_lag d on d.visit_id = a.id
            WHERE b.visit_id is null
            AND a.last_visit between @startdate and @enddate
            GROUP BY a.visitor_id
            HAVING `visitEnd` < DATE_ADD(@enddate, INTERVAL -2 WEEK)
            AND `visits` > 1
            ;
        
	    DROP TABLE IF EXISTS tmp_prediction_none_sale_pull_product{productId};
            ";
            //int[] eventssplit = Array.ConvertAll(selectEventsInstantiate().Split(','), int.Parse);
            Dictionary<int, int> eventsTrackedByClient = assignEventFactors();
            List<int> noneSaleEvents = getNoneSaleEvents();
            List<PredictionData> noneSaleVisits = new List<PredictionData>();
            //Open connection
            if (openConnection() == true)
            {
                Console.WriteLine("\t Pull None Sales Visits...");
                //Create Command
                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@startdate", startDate);
                    cmd.Parameters.AddWithValue("@enddate", endDate);
                    cmd.CommandTimeout = 0;
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        PredictionData visitor = new PredictionData(
                                        eventsTrackedByClient,
                                        noneSaleEvents,
                                        dataReader.GetInt32("visitor"),
                                        dataReader["visit_ids"].ToString(),
                                        dataReader["events"].ToString(),
                                        dataReader.GetInt32("views"),
                                        dataReader.GetInt32("visits"),
                                        dataReader.GetInt32("duration"),
                                        dataReader.GetDateTime("visitStart"),
                                        dataReader.GetDateTime("visitEnd"),
                                        false,
                                        frequency
                        );
                        // Assign variables here
                        noneSaleVisits.Add(visitor);
                    }
                    //close Data Reader
                    dataReader.Close();
                }
                //close Connection
                closeConnection();
                //return list to be displayed
                Console.WriteLine("\t\t Pulled " + (noneSaleVisits.Count));
                return noneSaleVisits;
            }
            else
            {
                return noneSaleVisits;
            }
        }
        public List<PredictionData> selectPredictionData(List<PredictionData> inVisitorLi, FrequencyData frequency, String product_id)
        {
            string table_token = Guid.NewGuid().ToString().Replace("-", "").ToLower();

            string query = $@"
			SET group_concat_max_len = 2000000;
            DROP TABLE IF EXISTS tmp_prediction_predict_pull_{product_id}_{table_token};

            CREATE TABLE IF NOT EXISTS tmp_prediction_predict_pull_{product_id}_{table_token} as (
                select 
                    a.visitor_id, a.id as `visit_id`,
                    a.first_visit, a.last_visit,
                    c.journey_id 
                from attrib_visit a
                LEFT JOIN agg_sales_temp b on b.visit_id = a.id
                LEFT JOIN agg_sales_temp_journeys c on c.visit_id = a.id
                WHERE a.last_visit between @startdate and @enddate
                AND b.visit_id is null
                group by a.visitor_id, a.id
                order by a.last_visit
            );

            SELECT
                a.visitor_id AS `visitor_id`,
                a.visit_id as `visit`,
                GROUP_CONCAT(b.id),
                GROUP_CONCAT(c.event_path) AS `events`,
                SUM(b.views) AS `views`,
                COUNT(b.id ) AS `visits`,
                TIMESTAMPDIFF(second, b.first_visit, b.last_visit) AS `duration`,
                MAX(b.first_visit) AS `start`,
                MAX(b.last_visit) AS `end`,
                a.journey_id,
                0 as `sale`	
            from tmp_prediction_predict_pull_{product_id}_{table_token} a
            join attrib_visit b on b.visitor_id = a.visitor_id
            LEFT JOIN agg_event_funnel_visit c on c.visit_id = b.id
            JOIN agg_prediction_lag d on d.visit_id = b.id
            LEFT JOIN agg_sales_temp_journeys e on e.visit_id = b.id
            LEFT JOIN agg_sales_temp f on f.visit_id = b.id
            WHERE a.last_visit >= b.last_visit
            AND f.visit_id is null
            AND (a.journey_id = e.journey_id or a.journey_id is null)
            group by a.visitor_id, a.journey_id, a.visit_id
            ;

            DROP TABLE IF EXISTS tmp_prediction_predict_pull_{product_id}_{table_token};
            ";
            //int[] eventssplit = Array.ConvertAll(selectEventsInstantiate().Split(','), int.Parse);
            Dictionary<int, int> eventsTrackedByClient = assignEventFactors();
            List<int> noneSaleEvents = getNoneSaleEvents();
            //Open connection
            if (openConnection() == true)
            {
                Console.WriteLine("\t Pull Daily Visits...");
                //Create Command
                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@startdate", startDate);
                    cmd.Parameters.AddWithValue("@enddate", endDate);
                    cmd.CommandTimeout = 0;
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        PredictionData visitor = new PredictionData(
                            eventsTrackedByClient,
                            noneSaleEvents,
                            dataReader.GetInt32("visitor_id"),
                            dataReader["visit"].ToString(),
                            dataReader["events"].ToString(),
                            dataReader.GetInt32("views"),
                            dataReader.GetInt32("visits"),
                            dataReader.GetInt32("duration"),
                            dataReader.GetDateTime("start"),
                            dataReader.GetDateTime("end"),
                            dataReader.GetBoolean("sale"),
                            frequency
                        );
                        inVisitorLi.Add(visitor);
                    }
                    //close Data Reader
                    dataReader.Close();
                }
                //close Connection
                closeConnection();
                Console.WriteLine("\t\t Pulled " + inVisitorLi.Count + ".");
                Console.WriteLine("\t\t Success.");
                //return list to be displayed
                return inVisitorLi;
            }
            else
            {
                return inVisitorLi;
            }
        }
        public List<PredictionData> selectPredictionDataSync(List<PredictionData> inVisitorLi, FrequencyData frequency, String product_id)
        {
            string query = $@"
		SELECT 
		    a.visitor_id AS `visitor_id`,
		    a.visit_id as `visit`,
		    GROUP_CONCAT(c.event_path) AS `events`,
		    SUM(b.views) AS `views`,
		    COUNT(b.visit_id) AS `visits`,
		    SUM(d.duration) AS `duration`,
		    MAX(b.visit_start) as `start`,
		    MAX(b.visit_end) as `end`,
		    b.journey_id,
		    a.sale as `sale`
		FROM agg_prediction_journey_syncs a
		JOIN agg_prediction_journey_syncs b on b.visitor_id = a.visitor_id
		LEFT JOIN agg_event_funnel_visit c on c.visit_id = b.visit_id
		JOIN agg_prediction_lag d on d.visit_id = b.visit_id
		WHERE (a.journey_id = b.journey_id OR b.journey_id IS NULL)
		AND a.visit_end >= b.visit_end
		AND b.sale = 0 AND a.sale = 0
		GROUP BY
		a.journey_id,
		a.visit_id order by a.sync_id,
		max(b.visit_end) asc
			";
            //int[] eventssplit = Array.ConvertAll(selectEventsInstantiate().Split(','), int.Parse);
            Dictionary<int, int> eventsTrackedByClient = assignEventFactors();
            List<int> noneSaleEvents = getNoneSaleEvents();
            //Open connection
            if (openConnection() == true)
            {
                Console.WriteLine("\t Pull Daily Visits...");
                //Create Command
                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@startdate", startDate);
                    cmd.Parameters.AddWithValue("@enddate", endDate);
                    cmd.CommandTimeout = 0;
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        PredictionData visitor = new PredictionData(
                                        eventsTrackedByClient,
                                        noneSaleEvents,
                                        dataReader.GetInt32("visitor_id"),
                                        dataReader["visit"].ToString(),
                                        dataReader["events"].ToString(),
                                        dataReader.GetInt32("views"),
                                        dataReader.GetInt32("visits"),
                                        dataReader.GetInt32("duration"),
                                        dataReader.GetDateTime("start"),
                                        dataReader.GetDateTime("end"),
                                        dataReader.GetBoolean("sale"),
                                        frequency
                        );
                        inVisitorLi.Add(visitor);
                    }
                    //close Data Reader
                    dataReader.Close();
                }
                //close Connection
                closeConnection();
                Console.WriteLine("\t\t Pulled " + inVisitorLi.Count + ".");
                Console.WriteLine("\t\t Success.");
                //return list to be displayed
                return inVisitorLi;
            }
            else
            {
                return inVisitorLi;
            }
        }
        public void insertLiklihood(List<PredictionData> vists)
        {
            string query = "INSERT IGNORE INTO agg_prediction_likelihood(visit_id, visit_date, product_id, likelihood) VALUES ";
            string handle_duplicate = @"ON DUPLICATE KEY UPDATE likelihood = VALUES(likelihood);";
            string format = "yyyy-MM-dd HH:mm:ss";
            StringBuilder sCommand = new StringBuilder(query);
            List<String> rows = new List<String>();

            if (openConnection() == true)
            {
                using (connection)
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = query;

                        foreach (var visit in vists)
                        {
                            rows.Add(
				                $"('{visit.visitIds.Max()}', '{visit.visitEnd.ToString(format)}', '{productId}', '{visit.likelihood}')"
                            );
                            if (rows.Count >= 10000)
                            {

                                sCommand.Append(string.Join(",", rows));
                                sCommand.Append(handle_duplicate);
                                cmd.CommandText = sCommand.ToString();
                                cmd.ExecuteNonQuery();
                                sCommand = new StringBuilder(query);
                                rows = new List<String>();
                            }
                        }
                        sCommand = new StringBuilder(query);
                        sCommand.Append(string.Join(",", rows));
                        sCommand.Append(handle_duplicate);
                        cmd.CommandText = sCommand.ToString();
                        cmd.ExecuteNonQuery();
                        rows = new List<String>();
                    }
                }
            }
        }

        public void insertWeighting(List<int> factorIDs, double[] weights, List<double> accuracy, string endDate)
        {
            List<string> rows = new List<string>();
            StringBuilder sCommand;
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string query = "INSERT IGNORE INTO agg_prediction_weights (`product_id`, `factor_id`, `weight`, `correct_percentage`, `false_percentage`, `created`) VALUES ";
            //add the b-constant
            rows.Add($"('{productId}', '1', '{weights[0]}', '{accuracy[0]}', '{accuracy[1]}', '{now}')");
            for (int i = 0; i <= factorIDs.Count - 1; i++)
            {
                rows.Add($"('{productId}', '{factorIDs[i]}', '{weights[i + 1]}', '{accuracy[0]}', '{accuracy[1]}', '{now}')");
            }
            sCommand = new StringBuilder(query);
            sCommand.Append(string.Join(",", rows));
            sCommand.Append(";");
            if (openConnection())
            {
                using (MySqlCommand cmd = new MySqlCommand(sCommand.ToString(), connection))
                {
                    cmd.CommandTimeout = 0;
                    cmd.ExecuteNonQuery();
                }
                closeConnection();
            }
        }
        public double[] getWeights(string startDate)
        {
            List<double> weights = new List<double>();
            int numberOfTrackedFactors = getInsertFactorIDs().Count();
            string query = $@"
			SELECT
				a.id,
				a.product_id,
				c.name,
				a.factor_id,
				a.weight,
				a.created
			FROM agg_prediction_weights a
			JOIN (
				SELECT
				a.product_id,
				MAX(a.created) AS `last_train_date`
				FROM agg_prediction_weights a
				group by a.product_id
				ORDER BY a.created desc
			) b ON a.created = b.last_train_date
			AND a.product_id = b.product_id
			JOIN agg_prediction_factor c ON a.factor_id = c.id
			WHERE a.product_id = {productId}
			GROUP BY a.factor_id
			";
            if (openConnection())
            {
                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.CommandTimeout = 0;
                    cmd.Parameters.AddWithValue("@startdate", startDate);
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        weights.Add(dataReader.GetDouble("weight"));
                    }
                    dataReader.Close();
                }
            }
            closeConnection();

            return weights.ToArray();

        }
        public Dictionary<int, int> assignEventFactors()
        {
            Dictionary<int, int> assignedEvents = new Dictionary<int, int>();
            string query = @"
			SELECT
			a.id,
			a.event_id
			FROM agg_prediction_factor a
			WHERE a.tracked = 1";
            if (openConnection())
            {
                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.CommandTimeout = 0;
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        assignedEvents.Add(dataReader.GetInt16("id"), dataReader.GetInt16("event_id"));
                    }
                    dataReader.Close();
                }
            }
            closeConnection();
            return assignedEvents;
        }
        public List<int> getInsertFactorIDs()
        {
            List<int> noneSaleFactorIDs = new List<int>();
            string query = $@"
                SELECT 
	                a.id, a.name, a.event_id
                FROM agg_prediction_factor a
                WHERE a.tracked = 1 and a.event_id = 0
                GROUP BY a.id

                UNION

                SELECT 
	                a.id, a.name, a.event_id
                FROM agg_prediction_factor a
                LEFT JOIN attrib_product_events b on a.event_id = b.event_id
                JOIN attrib_product c on c.id = b.product_id
                WHERE b.product_id = {productId} 
                    AND b.sale = 0
                    AND b.active = 1 
                    AND c.active = 1 
                    AND b.event_id > 0
                GROUP BY a.id
                ;";
            if (openConnection())
            {
                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.CommandTimeout = 0;
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        noneSaleFactorIDs.Add(dataReader.GetInt16("id"));
                    }
                    dataReader.Close();
                }
            }
            closeConnection();
            return noneSaleFactorIDs;
        }
        public List<int> getNoneSaleEvents()
        {
            List<int> noneSaleEvents = new List<int>();
            string query = $@"
            SELECT
                a.event_id as `event_id`,
                IFNULL(b.sale,0) as `sale`
            from agg_prediction_factor a 
            left join attrib_product_events b on a.event_id = b.event_id
            join attrib_product c on c.id = b.product_id
            WHERE (b.product_id = {productId} OR b.product_id is null) 
                and b.sale = 0
                and b.active = 1 
                and c.active = 1 
                and b.event_id > 0
            group by a.id;";
            if (openConnection())
            {
                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.CommandTimeout = 0;
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        noneSaleEvents.Add(dataReader.GetInt16("event_id"));
                    }
                    dataReader.Close();

                }
            }
            closeConnection();
            return noneSaleEvents;
        }
        public void getFrequencyData(String factor, out int minValues, out Int32 maxValues)
        {
            string query = $"select a.min_value as `minValues`, a.max_value as `maxValues` FROM agg_prediction_frequency a WHERE a.factor = '{factor}'";
            minValues = 0;
            maxValues = 0;

            if (openConnection() == true)
            {
                Console.WriteLine($"\t\t : {factor}");
                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        if (!dataReader.IsDBNull(0))
                        {
                            minValues = (dataReader.GetInt32("minValues"));
                        }
                        minValues = (dataReader.IsDBNull(0) ? 0 : dataReader.GetInt32("minValues"));
                        maxValues = (dataReader.GetInt32("maxValues"));
                    }
                    dataReader.Close();
                }
                closeConnection();

            }
        }
        public void getEventFrequencyData(out Dictionary<int, int> minValues, out Dictionary<int, int> maxValues)
        {
            string query = "select a.event_id as `event_id`, a.min_value as `minValues`, a.max_value as `maxValues` FROM agg_prediction_frequency a WHERE a.factor = 'event'";
            minValues = new Dictionary<int, int>();
            maxValues = new Dictionary<int, int>();

            if (openConnection() == true)
            {
                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        minValues.Add(dataReader.GetInt16("event_id"), dataReader.IsDBNull(0) ? 0 : dataReader.GetInt32("minValues"));
                        maxValues.Add(dataReader.GetInt16("event_id"), dataReader.GetInt32("maxValues"));
                    }
                    dataReader.Close();
                }
                closeConnection();

            }
        }
        //open connection to database
        private bool openConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }
        //Close connection
        private bool closeConnection()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
