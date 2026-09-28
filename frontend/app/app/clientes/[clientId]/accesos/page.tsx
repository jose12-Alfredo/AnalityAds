"use client";

import { useParams } from "next/navigation";
import { ClientAccessPanel } from "@/components/client-access-panel";

export default function ClientAccessPage() {
  const params = useParams<{ clientId: string }>();
  return <ClientAccessPanel clientId={params.clientId} />;
}
