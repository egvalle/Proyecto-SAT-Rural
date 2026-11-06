package com.satrural.alert_service.repository;

import com.satrural.alert_service.domain.ClimateEvent;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;

public interface ClimateEventRepository
    extends JpaRepository<ClimateEvent, Long>, JpaSpecificationExecutor<ClimateEvent> {}
