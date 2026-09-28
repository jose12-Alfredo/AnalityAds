"use client";

import { useParams } from "next/navigation";
import { ClientManagement } from "@/components/client-management";

export default function ClientDetailPage() { const params = useParams<{ clientId: string }>(); return <ClientManagement initialClientId={params.clientId} />; }
