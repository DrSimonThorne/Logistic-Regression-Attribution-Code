DELIMITER //
DROP PROCEDURE IF EXISTS sp_update_prediction_delta //
CREATE PROCEDURE sp_update_prediction_delta(
	IN startDate DATETIME,
	IN endDate DATETIME
)
BEGIN
	CREATE TABLE IF NOT EXISTS `agg_prediction_delta` (
		`id` INT(11) NOT NULL AUTO_INCREMENT,
		`referrer` VARCHAR(4096) NOT NULL,
		`sale` TINYINT(1) NOT NULL,
		`sales_date` DATETIME NULL,
		`pattern_id` INT(11) NULL,
		`revenue` INT(11) NULL,
		`visit_id` INT(11) NOT NULL,
		`delta` DECIMAL(12,10) NULL DEFAULT NULL,
		`liklihood` DECIMAL(12,10) NULL DEFAULT NULL,
		`visitor_id` INT(11) NOT NULL,
		`visit_in_sale` INT(11) NOT NULL,
		PRIMARY KEY (`id`),
		INDEX `visitor_id_refs_id_Y42r5xzc` (`visit_id`),
		INDEX `visitor_id_refs_id_ciU74WaI` (`visitor_id`),
		CONSTRAINT `visitor_id_refs_id_Y42r5xzc` FOREIGN KEY (`visit_id`) REFERENCES `attrib_visit` (`id`),
		CONSTRAINT `visitor_id_refs_id_ciU74WaI` FOREIGN KEY (`visitor_id`) REFERENCES `attrib_visitor` (`id`)
	)
	COLLATE='utf8_general_ci'
	ENGINE=InnoDB
	AUTO_INCREMENT=1023
	;
	
	SET @num := 0;
	SET @visitorRef := '';
	SET @liklihood := 0;
        
	INSERT INTO agg_prediction_delta(referrer, sale, sales_date, pattern_id, revenue, visit_id, delta, liklihood, visitor_id, visit_in_sale)
		SELECT
			c.referrer as `referrer`,
			a.sale as `sale`,
			b.sales_date as `sales_date`,
			b.pattern_id as `pattern_id`,
			b.revenue as `revenue`,
			a.visit_id as `visit_id`,
			CASE WHEN (@visitor<>a.visitor_id) THEN a.current_liklihood ELSE CASE WHEN (a.delta IS NOT NULL) THEN a.delta ELSE a.current_liklihood END END AS `delta`,
			a.current_liklihood AS `liklihood`,
			@visitor:=a.visitor_id AS `visitor_id`,
			a.visit_in_sale as `visit_in_sale`
		FROM (
			SELECT 
				a1.*, 
				@num := IF(@visitorRef = a1.visitor_id, @num := @num + 1, 1) AS visit_in_sale,
				@visitorRef := a1.visitor_id AS visCount,
				a2.liklihood - @liklihood AS 'delta',
				IF(a1.sale = 1, @liklihood:=1, @liklihood:= a2.liklihood) AS 'current_liklihood'
		      FROM agg_train_table a1
		      LEFT JOIN agg_visit_liklihood_async a2 ON a2.visit_id = a1.visit_id
		      JOIN (
				SELECT 
					b1.visitor_id,
					b1.visit_id,
					        #Am i okay using transaction_id?
					b1.transaction_id
				FROM agg_train_table b1
				WHERE b1.sales_date BETWEEN startDate AND endDate
		      ) AS a3 ON a1.visitor_id = a3.visitor_id
		        WHERE a1.visit_id <= a3.visit_id AND a1.transaction_id = a3.transaction_id
		        ORDER BY a1.visitor_id, a1.visit_end ASC
		) a
		LEFT JOIN agg_sales b ON b.visit_id = a.visit_id
		JOIN attrib_visit c ON c.id = a.visit_id
		ORDER BY a.visitor_id, a.visit_start
		ON DUPLICATE KEY UPDATE agg_prediction_delta.id = agg_prediction_delta.id
		;		

	UPDATE agg_prediction_delta a 
		JOIN agg_sales b on a.visitor_id = b.visitor_id
		JOIN (
		SELECT 
		      max(a.sales_date) AS sales_date, 
		      a.visitor_id
		FROM agg_train_table a
		WHERE a.sales_date BETWEEN startDate AND endDate
		GROUP BY a.visitor_id
		) c ON b.visitor_id = c.visitor_id AND b.sales_date = c.sales_date 
		SET
		      a.sales_date = b.sales_date, 
		      a.pattern_id = b.pattern_id,
		      a.revenue = b.revenue
		WHERE a.pattern_id IS NULL
		AND a.revenue IS NULL 
		AND b.sales_date BETWEEN startDate AND endDate
	;
		
END //

DELIMITER ;