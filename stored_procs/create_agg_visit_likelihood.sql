CREATE TABLE IF NOT EXISTS `agg_visit_likelihood` (
	`visit_id` INT NULL,
	`visit_date` DATETIME NULL,
	`likelihood` DECIMAL(12,10) NULL,
   PRIMARY KEY (`visit_id`),
	UNIQUE INDEX `visit_id` (`visit_id`, `visit_date`),
	INDEX `agg_visit_likelihood_56syuqpn` (`visit_id`)
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB
;