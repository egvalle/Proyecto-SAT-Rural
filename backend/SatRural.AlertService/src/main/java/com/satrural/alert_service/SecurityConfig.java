package com.satrural.alert_service;

import java.nio.charset.StandardCharsets;
import java.util.Collection;
import java.util.LinkedHashSet;
import java.util.Set;
import javax.crypto.spec.SecretKeySpec;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.HttpMethod;
import org.springframework.security.config.Customizer;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.core.GrantedAuthority;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.oauth2.jose.jws.MacAlgorithm;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.security.oauth2.jwt.JwtClaimValidator;
import org.springframework.security.oauth2.jwt.JwtDecoder;
import org.springframework.security.oauth2.jwt.JwtValidators;
import org.springframework.security.oauth2.jwt.NimbusJwtDecoder;
import org.springframework.security.oauth2.server.resource.authentication.JwtAuthenticationConverter;
import org.springframework.security.web.SecurityFilterChain;

@Configuration
public class SecurityConfig {
    private static final String LEGACY_ROLE_CLAIM =
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    @Bean
    SecurityFilterChain securityFilterChain(HttpSecurity http) throws Exception {
        http.csrf(csrf -> csrf.disable());
        http.sessionManagement(session ->
            session.sessionCreationPolicy(SessionCreationPolicy.STATELESS));
        http.authorizeHttpRequests(auth -> auth
            .requestMatchers("/actuator/health", "/actuator/health/**", "/internal/alerts/evaluate")
                .permitAll()
            .requestMatchers(HttpMethod.POST, "/api/alert-rules/**").hasRole("ADMIN")
            .requestMatchers(HttpMethod.PUT, "/api/alert-rules/**").hasRole("ADMIN")
            .requestMatchers(HttpMethod.DELETE, "/api/alert-rules/**").hasRole("ADMIN")
            .requestMatchers(HttpMethod.PATCH, "/api/alerts/**")
                .hasAnyRole("ADMIN", "OPERATOR", "USER")
            .requestMatchers("/api/**").authenticated()
            .anyRequest().denyAll());
        http.oauth2ResourceServer(oauth2 -> oauth2.jwt(jwt ->
            jwt.jwtAuthenticationConverter(jwtAuthenticationConverter())));
        return http.build();
    }

    @Bean
    JwtDecoder jwtDecoder(
        @Value("${satrural.security.jwt-key}") String key,
        @Value("${satrural.security.issuer}") String issuer,
        @Value("${satrural.security.audience}") String audience
    ) {
        if (key.length() < 32) {
            throw new IllegalArgumentException("JWT_KEY must contain at least 32 characters.");
        }
        NimbusJwtDecoder decoder = NimbusJwtDecoder
            .withSecretKey(new SecretKeySpec(key.getBytes(StandardCharsets.UTF_8), "HmacSHA256"))
            .macAlgorithm(MacAlgorithm.HS256)
            .build();
        decoder.setJwtValidator(new org.springframework.security.oauth2.core.DelegatingOAuth2TokenValidator<>(
            JwtValidators.createDefaultWithIssuer(issuer),
            new JwtClaimValidator<Collection<String>>("aud", values ->
                values != null && values.contains(audience))
        ));
        return decoder;
    }

    private JwtAuthenticationConverter jwtAuthenticationConverter() {
        JwtAuthenticationConverter converter = new JwtAuthenticationConverter();
        converter.setJwtGrantedAuthoritiesConverter(this::authorities);
        return converter;
    }

    private Collection<GrantedAuthority> authorities(Jwt jwt) {
        Set<GrantedAuthority> authorities = new LinkedHashSet<>();
        addRole(authorities, jwt.getClaimAsString("role"));
        addRole(authorities, jwt.getClaimAsString(LEGACY_ROLE_CLAIM));
        Collection<String> roles = jwt.getClaimAsStringList("roles");
        if (roles != null) {
            for (String role : roles) addRole(authorities, role);
        }
        return authorities;
    }

    private void addRole(Set<GrantedAuthority> authorities, String role) {
        if (role != null && !role.isBlank()) {
            authorities.add(new SimpleGrantedAuthority("ROLE_" + role));
        }
    }
}
