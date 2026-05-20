FROM busybox:musl AS busybox
FROM quay.io/keycloak/keycloak:26.0
USER root
COPY --from=busybox /bin/wget /usr/local/bin/wget
USER keycloak
