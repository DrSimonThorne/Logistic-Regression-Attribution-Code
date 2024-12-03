CREATE TABLE `agg_prediction_likelihood` (
	`visit_id` INT(11) NOT NULL DEFAULT '0',
	`visit_date` DATETIME NULL DEFAULT NULL,
	`likelihood` DECIMAL(12,10) NULL DEFAULT NULL,
	PRIMARY KEY (`visit_id`),
	UNIQUE INDEX `visit_id` (`visit_id`, `visit_date`),
	INDEX `agg_visit_likelihood_56syuqpn` (`visit_id`)
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB
;
