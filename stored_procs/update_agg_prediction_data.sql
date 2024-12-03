DELIMITER //
DROP PROCEDURE IF EXISTS sp_update_prediction_data //

CREATE PROCEDURE sp_update_prediction_data()

BEGIN

CREATE TABLE IF NOT EXISTS `agg_prediction_data` (
                `id` INT(11) NOT NULL AUTO_INCREMENT,
                `visitor_id` INT(11) NOT NULL,
                `visit_id` INT(11) NOT NULL,
                `event_path` VARCHAR(4096) NOT NULL,
                `on_site_duration` BIGINT(21) NOT NULL,
                `views` INT(11) NOT NULL,
                `visits` BIGINT(21) NOT NULL,
                `visit_start` DATETIME NOT NULL,
                `visit_end` DATETIME NOT NULL,
                `sales_date` DATETIME NULL,
                `sale` TINYINT(1) NOT NULL DEFAULT 0,
                `sale_id` INT(11) NULL default NULL,
                PRIMARY KEY (`id`),
                UNIQUE INDEX `visitor_id` (`visitor_id`, `visit_end`),
                INDEX `agg_train_table_0ps4u7x5` (`visitor_id`),
                INDEX `agg_train_table_yunlp2c4` (`sale`),
                INDEX `agg_train_table_7uw1d5f3` (`visit_start`),
                INDEX `agg_train_table_mu0lnkt2` (`visit_end`),
                INDEX `agg_train_table_wtmo27c` (`sale_id`),
                CONSTRAINT `visitor_id_refs_id_93cdoyk` FOREIGN KEY (`visitor_id`) REFERENCES `attrib_visitor` (`id`)
        );

        SET SESSION group_concat_max_len = 4096;

      INSERT INTO agg_prediction_data(visitor_id, visit_id, event_path, on_site_duration, views, visits, visit_start, visit_end, sale, sale_id)
        #nosales
                SELECT 
                        a.id AS visitor_id, 
                        group_concat(distinct(b.id)) as visit_id, 
                        group_concat(d.event_path) as event_path,
                        SUM(TIMESTAMPDIFF(SECOND, b.first_visit, b.last_visit)) as `on_site_duration`,
                        sum(b.views) as views,
                        count(distinct(b.id)) AS visits,
                        MIN(b.first_visit) AS visit_start,
                        MAX(b.last_visit) AS visit_end,
                        0 as sale,
                        NULL
                FROM attrib_visit b
                JOIN attrib_visitor a ON a.id = b.visitor_id
                JOIN attrib_pattern c ON b.pattern_id = c.id
                JOIN agg_event_funnel_visit d ON d.visit_id = b.id
                WHERE b.id NOT IN (
                        SELECT e.visit_id
                        FROM agg_sales e
                        where e.sales_date < enddate
                )
                AND b.first_visit BETWEEN startdate AND enddate
                GROUP BY b.id
               ON DUPLICATE KEY UPDATE sale = sale
        ;

        INSERT INTO agg_prediction_data(visitor_id, visit_id, event_path, on_site_duration, views, visits, visit_start, visit_end, sales_date, sale, sale_id)
                SELECT 
                        a.id AS visitor_id, 
                        group_concat(distinct(b.id)) as visit_id, 
                        group_concat(d.event_path) as event_path,
                        SUM(TIMESTAMPDIFF(SECOND, b.first_visit, b.last_visit)) as `on_site_duration`,
                        sum(b.views) as views,
                        count(distinct(b.id)) AS visits,
                        MIN(b.first_visit) AS visit_start,
                        MAX(b.last_visit) AS visit_end,
                        e.sales_date as sales_date,
                        1 as sale,
                        e.id as `sale_id`
                FROM attrib_visit b
                JOIN attrib_visitor a ON a.id = b.visitor_id
                JOIN attrib_pattern c ON b.pattern_id = c.id
                JOIN agg_event_funnel_visit d ON d.visit_id = b.id
                JOIN agg_sales e ON e.visit_id = b.id
                WHERE e.sales_date BETWEEN startdate AND enddate
                AND b.first_visit BETWEEN startdate AND enddate 
                AND b.last_visit BETWEEN startdate AND enddate
                GROUP BY b.id                
                ON DUPLICATE KEY UPDATE sale = sale
        ;
        
        UPDATE agg_prediction_data a
                JOIN ( 
                     SELECT a.visitor_id, a.visit_id, b.id
                     FROM agg_prediction_data a JOIN agg_sales b ON b.visitor_id = a.visitor_id
                     WHERE a.visit_id <= b.visit_id AND a.visit_start BETWEEN startdate AND enddate AND b.sales_date BETWEEN startdate AND enddate
                     GROUP BY a.visitor_id, a.visit_id
                ) AS c 
                ON a.visitor_id = c.visitor_id 
                AND a.visit_id = c.visit_id
                SET a.sale_id = c.id
                WHERE a.visit_start BETWEEN startdate AND enddate
        ;

END //

DELIMITER ;
