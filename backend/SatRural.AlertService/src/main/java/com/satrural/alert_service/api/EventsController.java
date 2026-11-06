package com.satrural.alert_service.api;

import com.satrural.alert_service.domain.ClimateEvent;
import com.satrural.alert_service.repository.ClimateEventRepository;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneOffset;
import java.util.HashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import org.springframework.data.domain.Sort;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.client.RestClient;

@RestController
@RequestMapping("/api/events")
public class EventsController {
    private final ClimateEventRepository repository;
    private final RestClient communityClient;

    public EventsController(
        ClimateEventRepository repository,
        @Value("${satrural.community.base-url}") String communityBaseUrl
    ) {
        this.repository = repository;
        this.communityClient = RestClient.builder().baseUrl(communityBaseUrl).build();
    }

    @GetMapping
    public List<EventResponse> getEvents(
        @RequestParam(required = false) Integer communityId,
        @RequestParam(required = false) String eventType,
        @RequestParam(required = false) String level,
        @RequestParam(required = false) LocalDate from,
        @RequestParam(required = false) LocalDate to
    ) {
        Instant start = from == null ? null : from.atStartOfDay(ZoneOffset.UTC).toInstant();
        Instant end = to == null ? null : to.plusDays(1).atStartOfDay(ZoneOffset.UTC).toInstant();
        Specification<ClimateEvent> filters =
            (root, query, builder) -> builder.conjunction();
        if (communityId != null)
            filters = filters.and((root, query, builder) ->
                builder.equal(root.get("communityId"), communityId));
        if (eventType != null && !eventType.isBlank()) {
            String type = eventType.trim();
            filters = filters.and((root, query, builder) ->
                builder.equal(root.get("eventType"), type));
        }
        if (level != null && !level.isBlank()) {
            String normalizedLevel = level.trim().toUpperCase(Locale.ROOT);
            filters = filters.and((root, query, builder) ->
                builder.equal(root.get("level"), normalizedLevel));
        }
        if (start != null)
            filters = filters.and((root, query, builder) ->
                builder.greaterThanOrEqualTo(root.get("occurredAt"), start));
        if (end != null)
            filters = filters.and((root, query, builder) ->
                builder.lessThan(root.get("occurredAt"), end));
        List<ClimateEvent> events = repository.findAll(
            filters, Sort.by(Sort.Direction.DESC, "occurredAt"));
        Map<Integer, String> names = new HashMap<>();
        return events.stream()
            .map(event -> EventResponse.from(event,
                names.computeIfAbsent(event.getCommunityId(), this::communityName)))
            .toList();
    }

    @GetMapping("/{id}")
    public ResponseEntity<?> getById(@PathVariable long id) {
        ClimateEvent event = repository.findById(id).orElse(null);
        if (event == null) {
            return ResponseEntity.status(404).body(Map.of("message", "Evento no encontrado."));
        }
        return ResponseEntity.ok(EventResponse.from(event, communityName(event.getCommunityId())));
    }

    private String communityName(int id) {
        try {
            CommunityLookup community = communityClient.get()
                .uri("/api/communities/{id}", id)
                .retrieve()
                .body(CommunityLookup.class);
            if (community != null) return community.name();
        } catch (Exception ignored) {
            return "Comunidad " + id;
        }
        return "Comunidad " + id;
    }

    private record CommunityLookup(String name) {}

    public record EventResponse(
        Long id, Long alertId, Integer communityId, String communityName,
        String eventType, String description, Instant occurredAt,
        Integer sensorId, String level, Boolean isActive
    ) {
        static EventResponse from(ClimateEvent event, String communityName) {
            return new EventResponse(
                event.getId(), event.getAlertId(), event.getCommunityId(), communityName,
                event.getEventType(), event.getDescription(), event.getOccurredAt(),
                event.getSensorId(), event.getLevel(), event.isActive()
            );
        }
    }
}
