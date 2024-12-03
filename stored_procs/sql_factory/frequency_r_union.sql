DELIMITER //
DROP PROCEDURE IF EXISTS sp_update_prediction_frequency //

CREATE PROCEDURE sp_update_prediction_frequency(
        IN startdate DATETIME,
	    IN enddate DATETIME
)
BEGIN

	SET
		@lag := 2,
	
		@visits_sumx := 0,
		@visits_sumx2 := 0,
		@visits_n := 0,
		
		@views_sumx := 0,
		@views_sumx2 := 0,
		@views_n := 0,
		
		@on_site_duration_sumx := 0,
		@on_site_duration_sumx2 := 0,
		@on_site_duration_n := 0,
			
		group_concat_max_len = 200000
	;

	SELECT

		@visits_sumx := sum(a.visits_sumx) as `visits_sumx`,
		@visits_sumx2 := sum(a.visits_sumx2) as `visits_sumx2`,
		@visits_n := sum(a.visits_n) as `visits_n`,
		
		@views_sumx := sum(a.views_sumx) as `views_sumx`,
		@views_sumx2 := sum(a.views_sumx2) as `views_sumx2`,
		@views_n := sum(a.views_n) as `views_n`,
	
		@on_site_duration_sumx := sum(a.on_site_duration_sumx) as `on_site_duration_sumx`,
		@on_site_duration_sumx2 := sum(a.on_site_duration_sumx2) as `on_site_duration_sumx2`,
		@on_site_duration_n := sum(a.on_site_duration_n) as `on_site_duration_n`
	
	FROM (
		SELECT 
			SUM(a.visits) as `visits_sumx`,
			SUM(a.visits2) as `visits_sumx2`,
			COUNT(a.visits) as `visits_n`,

			SUM(a.views) as `views_sumx`,
			SUM(a.views2) as `views_sumx2`,
			COUNT(a.views) as `views_n`,
			
			SUM(a.on_site_duration) as `on_site_duration_sumx`,
			SUM(a.on_site_duration2) as `on_site_duration_sumx2`,
			COUNT(a.on_site_duration) as `on_site_duration_n`,
			
			a.sale
	
		FROM (
			SELECT
				a.visitor_id,
				SUM(b.visits) AS `visits`,
				SUM(b.visits) * SUM(b.visits) AS `visits2`,
				SUM(b.views) as `views`,
				SUM(b.views) * SUM(b.views) as `views2`,
				SUM(TIMESTAMPDIFF(SECOND, b.visit_start, b.visit_end)) as `on_site_duration`,
				SUM(TIMESTAMPDIFF(SECOND, b.visit_start, b.visit_end)) 
					* SUM(TIMESTAMPDIFF(SECOND, b.visit_start, b.visit_end)) as `on_site_duration2`,
				a.sale
			FROM agg_prediction_data a
			JOIN agg_prediction_data b on b.visitor_id = a.visitor_id
			WHERE a.visit_start BETWEEN DATE_ADD(@startdate, INTERVAL -@lag WEEK) AND DATE_ADD(@enddate, INTERVAL -@lag WEEK)
				AND a.visit_id >= b.visit_id
				AND a.sale_id IS NULL
				AND b.sale_id IS NULL
			GROUP BY a.visitor_id 
			
			UNION

			SELECT
				a.visitor_id,
				SUM(b.visits) AS `visits`,
				SUM(b.visits) * SUM(b.visits) AS `visits2`,
				SUM(b.views) as `views`,
				SUM(b.views) * SUM(b.views) as `views2`,
				SUM(TIMESTAMPDIFF(SECOND, b.visit_start, b.visit_end)) as `on_site_duration`,
				SUM(TIMESTAMPDIFF(SECOND, b.visit_start, b.visit_end)) 
					* SUM(TIMESTAMPDIFF(SECOND, b.visit_start, b.visit_end)) as `on_site_duration2`,
				a.sale
			FROM agg_prediction_data a
			JOIN agg_prediction_data b on b.visitor_id = a.visitor_id
			WHERE a.visit_start BETWEEN @startdate AND @enddate
				AND b.visit_id <= a.visit_id
				AND (a.sale_id = b.sale_id)
				AND a.sale = 1
			GROUP BY a.visitor_id
		) a
	)a
;

	SELECT

		@tot_events_count_sumx := sum(a.tot_events_count_sumx) as `@tot_events_count_sumx`,
		@tot_events_count_sumx2 := sum(a.tot_events_count_sumx2) as `@tot_events_count_sumx2`,
		@tot_events_count_n := sum(a.tot_events_count_n) as `@tot_events_count_n`
		

	FROM (
		SELECT
			SUM(a.event_count) as `tot_events_count_sumx`,
			SUM(a.event_count^2) as `tot_events_count_sumx2`,
			COUNT(a.event_count) as `tot_events_count_n`
		FROM (
			SELECT
				COUNT(c.event_id) AS `event_count`,
				COUNT(c.event_id) * COUNT(c.event_id) AS `eevent_count2`
			FROM agg_prediction_data a
			JOIN agg_prediction_data b on b.visitor_id = a.visitor_id
			JOIN attrib_event_item c on b.visit_id = c.visit_id
			WHERE a.visit_start BETWEEN DATE_ADD(@startdate, INTERVAL -@lag WEEK) AND DATE_ADD(@enddate, INTERVAL -@lag WEEK)
				AND a.visit_id >= b.visit_id
				AND a.sale_id IS NULL
				AND b.sale_id IS NULL
			GROUP BY b.visitor_id
	
			UNION

			SELECT
				COUNT(c.event_id) AS `event_count`,
				COUNT(c.event_id) * COUNT(c.event_id) AS `eevent_count2`
			FROM agg_prediction_data a
			JOIN agg_prediction_data b on b.visitor_id = a.visitor_id
			JOIN attrib_event_item c on b.visit_id = c.visit_id
			WHERE a.visit_start BETWEEN @startdate AND @enddate
				AND b.visit_id <= a.visit_id
				AND (a.sale_id = b.sale_id)
				AND a.sale = 1
			GROUP BY a.visitor_id
		) a
	) a
	;
	#
	# FIRST UPDATE THE SUM | SUM^2 | COUNT OF OBSERVATIONS
	#

	UPDATE agg_prediction_frequency a
		SET
			a.sum_values = a.sum_values + @visits_sumx,
	 		a.sum_values_sqr = a.sum_values_sqr + @visits_sumx2,
	 		a.count_values = a.count_values + @visits_n
		WHERE @visits_sumx > 0
		AND @visits_n > 0
		AND a.factor = 'visits'
	;

	UPDATE agg_prediction_frequency a
		SET
			a.sum_values = a.sum_values + @views_sumx,
	 		a.sum_values_sqr = a.sum_values_sqr + @views_sumx2,
	 		a.count_values = a.count_values + @views_n
		WHERE @visits_sumx > 0
		AND @visits_n > 0
		AND a.factor = 'views'
	;

	UPDATE agg_prediction_frequency a
		SET
			a.sum_values = a.sum_values + @on_site_duration_sumx,
	 		a.sum_values_sqr = a.sum_values_sqr + @on_site_duration_sumx2,
	 		a.count_values = a.count_values + @on_site_duration_n
		WHERE @visits_sumx > 0
		AND @visits_n > 0
		AND a.factor = 'on_site_duration'
	;
	
	UPDATE agg_prediction_frequency a
		SET
			a.sum_values = a.sum_values + @tot_events_count_sumx,
	 		a.sum_values_sqr = a.sum_values_sqr + @tot_events_count_sumx2,
	 		a.count_values = a.count_values + @tot_events_count_n
		WHERE @visits_sumx > 0
		AND @visits_n > 0
		AND a.factor = 'events_count'
	;

	#
	# THEN CALCULATE THE STDDEV + MEAN
	#

	UPDATE agg_prediction_frequency a
		JOIN ( SELECT * FROM agg_prediction_frequency a) b on a.id = b.id
		SET
			a.mean_values = b.sum_values / b.count_values
		WHERE a.id = b.id
		AND b.sum_values > 0
		AND b.count_values > 0
	;
	UPDATE agg_prediction_frequency a
		JOIN ( SELECT * FROM agg_prediction_frequency a) b on a.id = b.id
		SET
			a.std_value = SQRT(b.sum_values_sqr / b.count_values - (b.mean_values * b.mean_values))
		WHERE a.id = b.id
		AND b.sum_values > 0
		AND b.count_values > 0
	;

	#
	# FINALLY USE THE STDDEV TO SCOPE OUT THE OUTLIERS
	#

	#views
	UPDATE agg_prediction_frequency a
		JOIN (
			SELECT
				MIN(a.`views`) as min_views,
				MAX(a.`views`) as max_views
			FROM (
				SELECT
					SUM(b.views) as views
				FROM agg_prediction_data a
				JOIN agg_prediction_data b ON a.visitor_id = b.visitor_id
				WHERE a.visit_start BETWEEN startdate AND enddate
				AND b.visit_id <= a.visit_id
				AND (a.sale_id = b.sale_id OR b.sale_id = NULL)
				GROUP BY a.visitor_id, a.visit_id
			) a
			JOIN agg_prediction_frequency c on c.factor = 'views'
			WHERE a.`views` BETWEEN (c.mean_values - 3 * c.std_value) AND (c.mean_values + 3 * c.std_value)
		) b on a.factor = 'views'
		SET 
			a.min_value = CASE WHEN a.min_value IS NULL THEN b.min_views ELSE LEAST(a.min_value, b.min_views) END,
			a.max_value = GREATEST(a.max_value, b.max_views),
			a.last_updated = CASE WHEN a.min_value < b.min_views OR a.max_value < b.max_views  THEN startdate END
		WHERE a.factor = 'views'
		AND b.min_views >= 0
		AND b.max_views >= 0
	;

	#visits
	UPDATE agg_prediction_frequency a
		JOIN (
			SELECT
				MIN(a.`visits`) as min_visits,
				MAX(a.`visits`) as max_visits
			FROM (
				SELECT
         		COUNT(DISTINCT(b.visit_id)) as visits
				FROM agg_prediction_data a
				JOIN agg_prediction_data b ON a.visitor_id = b.visitor_id
				WHERE a.visit_start BETWEEN startdate AND enddate
				AND b.visit_id <= a.visit_id
				AND (a.sale_id = b.sale_id OR b.sale_id = NULL)
				GROUP BY a.visitor_id, a.visit_id
			) a
			JOIN agg_prediction_frequency c on c.factor = 'visits'
			WHERE a.`visits` BETWEEN (c.mean_values - 3 * c.std_value) AND (c.mean_values + 3 * c.std_value)
		) b on a.factor = 'visits'
	
		SET
			a.min_value = CASE WHEN a.min_value IS NULL THEN b.min_visits ELSE LEAST(a.min_value, b.min_visits) END,
			a.max_value = GREATEST(a.max_value, b.max_visits),
      		a.last_updated = CASE WHEN a.min_value < b.min_visits OR a.max_value < b.max_visits THEN startdate END
		WHERE a.factor = 'visits'
		AND b.min_visits >= 0
		AND b.max_visits >= 0
	;
	#on_site_duration
	UPDATE agg_prediction_frequency a
		JOIN (
			SELECT
				MIN(a.`on_site_duration`) as min_on_site_duration,
				MAX(a.`on_site_duration`) as max_on_site_duration
			FROM (
				SELECT
					SUM(TIMESTAMPDIFF(SECOND, b.visit_start, b.visit_end)) as `on_site_duration`
				FROM agg_prediction_data a
				JOIN agg_prediction_data b ON a.visitor_id = b.visitor_id
				WHERE a.visit_start BETWEEN startdate AND enddate
				AND b.visit_id <= a.visit_id
				AND (a.sale_id = b.sale_id OR b.sale_id = NULL)
				GROUP BY a.visitor_id, a.visit_id
			) a
			JOIN agg_prediction_frequency c on c.factor = 'on_site_duration'
			WHERE a.`on_site_duration` BETWEEN (c.mean_values - 3 * c.std_value) AND (c.mean_values + 3 * c.std_value)
		) b on a.factor = 'on_site_duration'
	
		SET
			a.min_value = CASE WHEN a.min_value IS NULL THEN b.min_on_site_duration ELSE LEAST(a.min_value, b.min_on_site_duration) END,
			a.max_value = GREATEST(a.max_value, b.max_on_site_duration),
			a.last_updated = CASE WHEN a.min_value < b.min_on_site_duration OR a.max_value < b.max_on_site_duration  THEN startdate END
		WHERE a.factor = 'on_site_duration'
		AND b.min_on_site_duration >= 0
		AND b.max_on_site_duration >= 0
	;
	#events_count
	UPDATE agg_prediction_frequency a
		JOIN (
			SELECT
				MIN(a.`count`) as min_events_count,
				MAX(a.`count`) as max_events_count
			FROM (
			SELECT
				c.event_id,
				COUNT(c.event_id) AS `count`
				FROM agg_prediction_data a
				JOIN agg_prediction_data b ON a.visitor_id = b.visitor_id
				JOIN attrib_event_item c on b.visit_id = c.visit_id
				WHERE a.visit_start BETWEEN startdate AND enddate
				AND b.visit_id <= a.visit_id
				AND (a.sale_id = b.sale_id OR b.sale_id = NULL)
				GROUP BY a.visitor_id, a.visit_id
			) a
			JOIN agg_prediction_frequency c on c.factor = 'events_count'
			WHERE a.`count` BETWEEN (c.mean_values - 3 * c.std_value) AND (c.mean_values + 3 * c.std_value)
		) b on a.factor = 'events_count'
		SET
			a.min_value = CASE WHEN a.min_value IS NULL THEN b.min_events_count ELSE LEAST(a.min_value, b.min_events_count) END,
			a.max_value = GREATEST(a.max_value, b.max_events_count),
		  a.last_updated = CASE WHEN a.min_value < b.min_events_count OR a.max_value < b.max_events_count  THEN startdate END
		WHERE a.factor = 'events_count'
		AND b.min_events_count >= 0
		AND b.max_events_count >= 0
	;
	#events
	UPDATE agg_prediction_frequency a
		JOIN (
			SELECT
				a.event_id,
				MIN(a.`count`) as min_events_count,
				MAX(a.`count`) as max_events_count
			FROM (
				SELECT
					c.event_id, COUNT(c.event_id) AS `count`
				FROM agg_prediction_data a
				JOIN agg_prediction_data b ON a.visitor_id = b.visitor_id
				JOIN attrib_event_item c on b.visit_id = c.visit_id
				WHERE a.visit_start BETWEEN startdate AND enddate
				AND b.visit_id <= a.visit_id
				AND (a.sale_id = b.sale_id OR b.sale_id = NULL)
				GROUP BY a.visitor_id, a.visit_id
			) a
			JOIN agg_prediction_frequency c on c.event_id = a.event_id
			WHERE a.`count` BETWEEN (c.mean_values - 3 * c.std_value) AND (c.mean_values + 3 * c.std_value)
			group by a.event_id
		) b on a.event_id = b.event_id
	
		SET
			a.min_value = LEAST(a.min_value, b.min_events_count),
			a.max_value = GREATEST(a.max_value, b.max_events_count),
		  a.last_updated = CASE WHEN a.min_value < b.min_events_count OR a.max_value < b.max_events_count  THEN startdate END
		WHERE a.factor = 'event'
		AND a.event_id = b.event_id
		AND b.min_events_count >= 0
		AND b.max_events_count >= 0
;
END //

DELIMITER ;