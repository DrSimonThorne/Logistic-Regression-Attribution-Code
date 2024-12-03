##SALES DATA PULL
SELECT a.visitor_id AS `visitor`,
    GROUP_CONCAT(b.visit_id ORDER BY b.visit_id) AS `visit`,
    GROUP_CONCAT(b.event_path ORDER BY b.visit_id) AS `events`,
    SUM(b.on_site_duration) AS `duration`,
    SUM(b.views) AS `views`,
    SUM(b.visits) AS `visits`,
    MIN(b.visit_start) AS `start`,
    MAX(b.visit_end) AS`end`,
    MAX(b.sales_date),
    a.sale_id
FROM agg_prediction_data a
JOIN agg_prediction_data b ON a.visitor_id = b.visitor_id
WHERE a.visit_start BETWEEN @startdate AND @enddate
  AND b.visit_id <= a.visit_id
  AND (a.sale_id = b.sale_id)
  AND a.sale = 1
GROUP BY a.visitor_id,
         a.visit_id
ORDER BY a.visitor_id,
         a.visit_start
;

##NONE SALES DATA PULL
SELECT a.visitor_id AS `visitor`,
       group_concat(b.visit_id) AS `visitID`,
       group_concat(b.event_path) AS `events`,
       sum(b.views) AS `views`,
       sum(b.visits) AS `visits`,
       sum(b.on_site_duration) AS `duration`,
       min(b.visit_start) AS `visitStart`,
       max(b.visit_end) AS `visitEnd`,
       a.sale_id
FROM agg_prediction_data a
JOIN agg_prediction_data b ON a.visitor_id = b.visitor_id
WHERE a.visit_start BETWEEN @startdate AND @enddate
  AND b.visit_start BETWEEN @startdate AND @enddate
  AND a.visit_id >= b.visit_id
  AND a.sale_id IS NULL
GROUP BY a.visitor_id

##DATA PULL FOR PREDICTION
SELECT a.visitor_id AS `visitor_id`,
       a.visit_id AS `visit`,
       GROUP_CONCAT(b.event_path ORDER BY b.visit_id) AS `events`,
       SUM(b.views) AS `views`,
       SUM(b.visits) AS `visits`,
       SUM(b.on_site_duration) AS `duration`,
       MIN(a.visit_start) AS `start`,
       MAX(a.visit_end) AS `end`,
       a.sale_id,
       MAX(b.sales_date) AS `sales`,
       b.sales_date
FROM agg_prediction_data a
JOIN agg_prediction_data b ON a.visitor_id = b.visitor_id
WHERE a.visit_start BETWEEN @startday AND @enddate
  AND b.visit_id AS <= a.visit_id
  AND (b.sale_id = a.sale_id
     OR b.sale_id IS NULL
  )
  AND a.sale = 0
GROUP BY a.visitor_id,
         a.visit_id
ORDER BY a.visitor_id,
         a.visit_start;