DELIMITER //
DROP PROCEDURE IF EXISTS sp_create_prediction_frequency //

CREATE PROCEDURE sp_create_prediction_frequency()

BEGIN

	CREATE TABLE `agg_prediction_frequency` (
		`id` INT(11) NOT NULL AUTO_INCREMENT,
		`factor` VARCHAR(30) NOT NULL,
		`event_id` INT(11) NULL DEFAULT NULL,
		`min_value` INT(11) NULL DEFAULT '0',
		`max_value` INT(11) NULL DEFAULT '0',
		`sum_values` INT(11) NOT NULL DEFAULT '0',
		`sum_values_sqr` BIGINT(20) UNSIGNED NOT NULL DEFAULT '0',
		`count_values` INT(11) NOT NULL DEFAULT '0',
		`mean_values` FLOAT NOT NULL DEFAULT '0',
		`std_value` FLOAT NOT NULL DEFAULT '0',
		`last_updated` DATE NOT NULL DEFAULT '0000-00-00',
		PRIMARY KEY (`id`)
	)
	
	COLLATE='utf8_general_ci'
	ENGINE=InnoDB
	;

	INSERT INTO agg_prediction_frequency(factor)
		VALUES	
			('on_site_duration'),
			('duration_between_sale'),
			('views'),
			('visits'),
			('events_count'),
			('previous_sale_count')
	;
		
	INSERT INTO agg_prediction_frequency(factor, event_id)
		SELECT 
		'event',
		a.id
		FROM attrib_event a
		ORDER BY a.id
	;
    
END //

DELIMITER ;
