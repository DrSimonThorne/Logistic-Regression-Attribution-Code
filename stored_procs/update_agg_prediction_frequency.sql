DELIMITER //
DROP PROCEDURE IF EXISTS sp_update_prediction_frequency //

CREATE PROCEDURE sp_update_prediction_frequency(
        IN startdate DATETIME,
	    IN enddate DATETIME
)
BEGIN

#
# FIRST UPDATE THE SUM | SUM^2 | COUNT OF OBSERVATIONS
#

#visits
UPDATE agg_prediction_frequency a
	JOIN (
        SELECT
            sum(a.visits) as sumx,
            sum(a.visits2) as sumx2,
            count(a.visits) as n
        FROM (
            SELECT
                a.visitor_id,
                a.visit_id,
                COUNT(b.visits) AS `visits`,
                COUNT(b.visits) * COUNT(b.visits) AS `visits2`
            FROM agg_prediction_data a
            JOIN agg_prediction_data b ON a.visitor_id = b.visitor_id
            WHERE a.visit_start BETWEEN startdate AND enddate
            AND b.visit_id <= a.visit_id
            AND (a.sale_id = b.sale_id OR b.sale_id = NULL)
            GROUP BY a.visitor_id, a.visit_id
        ) a
	) b on a.factor = 'visits'

SET
	a.sum_values = a.sum_values + b.sumx,
 	a.sum_values_sqr = a.sum_values_sqr + b.sumx2,
 	a.count_values = a.count_values + b.n
WHERE b.sumx > 0
AND b.n > 0
;

UPDATE agg_prediction_frequency a
	JOIN (
		SELECT
			sum(a.views) as sumx,
			sum(a.views2) as sumx2,
			count(a.views) as n
		FROM (
			SELECT
				SUM(b.views) as views,
				SUM(b.views) * SUM(b.views) as views2
			FROM agg_prediction_data a
            JOIN agg_prediction_data b ON a.visitor_id = b.visitor_id
            WHERE a.visit_start BETWEEN startdate AND enddate
            AND b.visit_id <= a.visit_id
            AND (a.sale_id = b.sale_id OR b.sale_id = NULL)
            GROUP BY a.visitor_id, a.visit_id
		) a
	) b on a.factor = 'views'

SET
	a.sum_values = a.sum_values + b.sumx,
 	a.sum_values_sqr = a.sum_values_sqr + b.sumx2,
 	a.count_values = a.count_values + b.n
WHERE b.sumx > 0
AND b.n > 0
;
#on_site_duration
UPDATE agg_prediction_frequency a
	JOIN (
		SELECT
            sum(a.on_site_duration) as sumx,
            sum(a.on_site_duration2) as sumx2,
            count(a.on_site_duration) as n
        FROM (
            SELECT
                SUM(TIMESTAMPDIFF(SECOND, b.visit_start, b.visit_end)) as `on_site_duration`,
                SUM(TIMESTAMPDIFF(SECOND, b.visit_start, b.visit_end)) * SUM(TIMESTAMPDIFF(SECOND, b.visit_start, b.visit_end)) as `on_site_duration2`
            FROM agg_prediction_data a
        JOIN agg_prediction_data b ON a.visitor_id = b.visitor_id
        WHERE a.visit_start BETWEEN startdate AND enddate
        AND b.visit_id <= a.visit_id
        AND (a.sale_id = b.sale_id OR b.sale_id = NULL)
        GROUP BY a.visitor_id, a.visit_id
        ) a
	) b on a.factor = 'on_site_duration'

SET
	a.sum_values = a.sum_values + b.sumx,
 	a.sum_values_sqr = a.sum_values_sqr + b.sumx2,
 	a.count_values = a.count_values + b.n
WHERE b.sumx > 0
AND b.n > 0
;

#events_count
UPDATE agg_prediction_frequency a
	JOIN (
		SELECT
			sum(a.event_count) as sumx,
			sum(a.event_count2) as sumx2,
			count(a.event_count) as n
		FROM (
			SELECT
				a.visitor_id, 
				COUNT(c.event_id) AS `event_count`,
				COUNT(c.event_id) * COUNT(c.event_id) AS `event_count2`
			FROM agg_prediction_data a
            JOIN agg_prediction_data b ON a.visitor_id = b.visitor_id
            JOIN attrib_event_item c on b.visit_id = c.visit_id
            WHERE a.visit_start BETWEEN startdate AND enddate
            AND b.visit_id <= a.visit_id
            AND (a.sale_id = b.sale_id OR b.sale_id = NULL)
            GROUP BY a.visitor_id, a.visit_id
		) a
	) b on a.factor = 'events_count'

SET
	a.sum_values = a.sum_values + b.sumx,
 	a.sum_values_sqr = a.sum_values_sqr + b.sumx2,
 	a.count_values = a.count_values + b.n
WHERE b.sumx > 0
AND b.n > 0
;

UPDATE agg_prediction_frequency a
	JOIN (
		SELECT
			a.event_id,
			sum(a.event_count) as sumx,
			sum(a.event_count2) as sumx2,
			count(a.event_count) as n
		FROM (
            SELECT
                c.event_id,
                COUNT(c.event_id) AS `event_count`,
                COUNT(c.event_id) * COUNT(c.event_id) AS `event_count2`
            FROM agg_prediction_data a
            JOIN agg_prediction_data b ON a.visitor_id = b.visitor_id
            JOIN attrib_event_item c on b.visit_id = c.visit_id
            WHERE a.visit_start BETWEEN startdate AND enddate
            AND b.visit_id <= a.visit_id
            AND (a.sale_id = b.sale_id OR b.sale_id = NULL)
            GROUP BY a.visitor_id, a.visit_id, c.event_id
		) a
		group by a.event_id
	) b on a.factor = 'event'
SET
	a.sum_values = a.sum_values + b.sumx,
 	a.sum_values_sqr = a.sum_values_sqr + b.sumx2,
 	a.count_values = a.count_values + b.n
WHERE a.event_id = b.event_id
AND b.sumx > 0
AND b.n > 0
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
# FINALLY USE THE STDDEV TO SCOPE OUT THE OUTLIERS AND UPDATE MIN MAX FROM RETURNED RESULTS.
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
